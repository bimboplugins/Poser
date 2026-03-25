using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility.Signatures;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control; 
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Poser;

public unsafe class Poser : IDalamudPlugin
{
    public string Name => "Poser";

    private IDalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    private IChatGui ChatGui { get; init; }
    private ICondition Condition { get; init; }
    private IObjectTable ObjectTable { get; init; }
    private IGameInteropProvider InteropProvider { get; init; }

    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("Poser");
    public PoserUi Window { get; init; }
    public ConfigWindow ConfigWindow { get; init; }

    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B F2 48 8B F9 45 84 C9")]
    private readonly delegate* unmanaged<IntPtr, IntPtr, IntPtr, byte, void> _processChatBox = null;

    public Poser(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        ICondition condition,
        IObjectTable objectTable,
        IGameInteropProvider interopProvider)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        ChatGui = chatGui;
        Condition = condition;
        ObjectTable = objectTable;
        InteropProvider = interopProvider;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        InteropProvider.InitializeFromAttributes(this);
        
        Window = new PoserUi(this);
        ConfigWindow = new ConfigWindow(this);
        
        WindowSystem.AddWindow(Window);
        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleUi;

        CommandManager.AddHandler("/poser", new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the UI, or set pose directly. Usage: /poser [idle/sit/gsit/doze] [index]"
        });
        
        CommandManager.AddHandler("/poserconfig", new CommandInfo(OnCommandConfig)
        {
            HelpMessage = "Opens the Poser configuration menu."
        });
        
        CommandManager.AddHandler("/poserdebug", new CommandInfo(OnCommandDebug)
        {
            HelpMessage = "Prints current memory pose state to chat."
        });
        
        CommandManager.AddHandler("/pidle", new CommandInfo(OnCommandPose) { HelpMessage = "Set idle pose. Usage: /pidle [index]" });
        CommandManager.AddHandler("/pgsit", new CommandInfo(OnCommandPose) { HelpMessage = "Set ground sit pose. Usage: /pgsit [index]" });
        CommandManager.AddHandler("/psit", new CommandInfo(OnCommandPose) { HelpMessage = "Set chair sit pose. Usage: /psit [index]" });
        CommandManager.AddHandler("/pdoze", new CommandInfo(OnCommandPose) { HelpMessage = "Set doze pose. Usage: /pdoze [index]" });
    }

    private void ToggleUi() => Window.IsOpen = !Window.IsOpen;
    private void ToggleConfigUi() => ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
    private void OnCommandConfig(string command, string args) => ToggleConfigUi();

    private void OnCommandDebug(string command, string args)
    {
        if (ObjectTable.LocalPlayer == null) return;
        var player = (Character*)ObjectTable.LocalPlayer.Address;
        
        ChatGui.Print($"[Poser Debug] CurrentPoseType Enum: {player->EmoteController.CurrentPoseType}");
        ChatGui.Print($"[Poser Debug] CurrentPoseType Byte: {(byte)player->EmoteController.CurrentPoseType}");
        ChatGui.Print($"[Poser Debug] CPoseState Index: {player->EmoteController.CPoseState}");
    }

    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            ToggleUi();
            return;
        }

        var splitArgs = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (splitArgs.Length == 2)
        {
            string type = splitArgs[0].ToLowerInvariant();
            if (type is not ("idle" or "sit" or "gsit" or "doze"))
            {
                ChatGui.PrintError("[Poser] Invalid stance. Use idle, sit, gsit, or doze.");
                return;
            }

            if (byte.TryParse(splitArgs[1], out byte index))
            {
                byte max = GetMaxPoseIndex(type);
                if (index > max)
                {
                    ChatGui.PrintError($"[Poser] Invalid index {index}. Your max for {type} is {max}.");
                    return;
                }

                ExecuteSyncPose(type, index);
            }
            else
            {
                ChatGui.PrintError("[Poser] Invalid index. Must be a number.");
            }
        }
        else
        {
            ChatGui.PrintError("[Poser] Invalid syntax. Usage: /poser [stance] [index] (or just /poser to open UI)");
        }
    }

    private void OnCommandPose(string command, string args)
    {
        string type = command switch
        {
            "/pidle" => "idle",
            "/pgsit" => "gsit",
            "/psit"  => "sit",
            "/pdoze" => "doze",
            _        => "idle"
        };

        if (string.IsNullOrWhiteSpace(args))
        {
            ChatGui.PrintError($"[Poser] Invalid syntax. Usage: {command} [index]");
            return;
        }

        if (byte.TryParse(args.Trim(), out byte index))
        {
            byte max = GetMaxPoseIndex(type);
            if (index > max)
            {
                ChatGui.PrintError($"[Poser] Invalid index {index}. Your max for {type} is {max}.");
                return;
            }

            ExecuteSyncPose(type, index);
        }
        else
        {
            ChatGui.PrintError("[Poser] Invalid index.");
        }
    }

    internal bool IsPoseActive(string type, byte index)
    {
        if (ObjectTable.LocalPlayer == null) return false;
        
        var player = (Character*)ObjectTable.LocalPlayer.Address;
        var targetPoseType = GetPoseType(type);

        // UI Highlighting reverts to strict checking to prevent the dual-highlight bug
        if (player->EmoteController.CurrentPoseType != targetPoseType) return false;
        
        return player->EmoteController.CPoseState == index;
    }

    internal void ExecuteSyncPose(string type, byte requestedIndex)
    {
        if (ObjectTable.LocalPlayer == null) return;

        if (Condition[ConditionFlag.OccupiedInCutSceneEvent] || 
            Condition[ConditionFlag.Unconscious] || 
            Condition[ConditionFlag.BetweenAreas] ||
            Condition[ConditionFlag.InCombat])
        {
            ChatGui.PrintError("[Poser] Cannot change pose right now.");
            return;
        }

        var player = (Character*)ObjectTable.LocalPlayer.Address;
        var targetPoseType = GetPoseType(type);
        var currentPoseType = player->EmoteController.CurrentPoseType;

        bool isCorrectStance = currentPoseType == targetPoseType;
        
        // Command Execution remains relaxed to bypass the game's ambiguous memory states
        if (targetPoseType == EmoteController.PoseType.Sit || targetPoseType == EmoteController.PoseType.GroundSit)
        {
            isCorrectStance = currentPoseType == EmoteController.PoseType.Sit || 
                              currentPoseType == EmoteController.PoseType.GroundSit || 
                              (byte)currentPoseType == 255;
        }

        if (!isCorrectStance)
        {
            ChatGui.PrintError($"[Poser] You must be in the {type} stance to change this pose.");
            return;
        }

        int currentPose = player->EmoteController.CPoseState;
        if (currentPose == requestedIndex) return; 

        int totalPoses = GetMaxPoseIndex(type) + 1; 
        int diff = (requestedIndex - currentPose + totalPoses) % totalPoses;

        if (_processChatBox != null)
        {
            for (int i = 0; i < diff; i++)
            {
                Utf8String message = new Utf8String();
                message.SetString("/cpose");
                _processChatBox((IntPtr)UIModule.Instance(), (IntPtr)(&message), IntPtr.Zero, 0);
                message.Dtor(); 
            }
        }
    }

    internal byte GetMaxPoseIndex(string type)
    {
        return EmoteController.GetAvailablePoses(GetPoseType(type));
    }

    private EmoteController.PoseType GetPoseType(string type)
    {
        switch (type)
        {
            case "gsit": return EmoteController.PoseType.GroundSit;
            case "sit":  return EmoteController.PoseType.Sit;
            case "doze": return EmoteController.PoseType.Doze;
            default:     return EmoteController.PoseType.Idle;
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleUi;
        
        WindowSystem.RemoveAllWindows();
        
        CommandManager.RemoveHandler("/poser");
        CommandManager.RemoveHandler("/poserconfig");
        CommandManager.RemoveHandler("/poserdebug");
        CommandManager.RemoveHandler("/pidle");
        CommandManager.RemoveHandler("/pgsit");
        CommandManager.RemoveHandler("/psit");
        CommandManager.RemoveHandler("/pdoze");
    }
}