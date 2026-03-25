using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace Poser;

public class PoserUi : Window, IDisposable
{
    private Poser Plugin { get; init; }

    public PoserUi(Poser plugin) : base(
        "Poser", 
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize) 
    {
        Plugin = plugin;
        RespectCloseHotkey = true;
        AllowPinning = false;
        AllowClickthrough = false;
        BgAlpha = plugin.Configuration.WindowOpacity;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 0), 
            MaximumSize = new Vector2(500, 1000)
        };

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            Click = _ => { Plugin.ConfigWindow.IsOpen = !Plugin.ConfigWindow.IsOpen; }
        });
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (Plugin.Configuration.LockWindow)
            Flags |= ImGuiWindowFlags.NoMove; 
        else
            Flags &= ~ImGuiWindowFlags.NoMove;
    }

    public override void Draw()
    {
        bool isFirst = true;

        foreach (var category in Plugin.Configuration.Categories)
        {
            if (category.Enabled)
            {
                if (!isFirst)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                }
                
                DrawPoseSection(category.Label, category.Type);
                isFirst = false;
            }
        }
    }

    private void DrawPoseSection(string label, string type)
    {
        float windowWidth = ImGui.GetWindowSize().X;
        float textWidth = ImGui.CalcTextSize(label).X;
        
        // Center text header
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
        ImGui.TextDisabled(label);
        
        // Fallback max pose limit
        byte maxPoses = Plugin.GetMaxPoseIndex(type);
        if (maxPoses == 0 || maxPoses > 10) maxPoses = 5; 
        
        string sentenceCaseType = type switch
        {
            "idle" => "Idle",
            "sit"  => "Sit",
            "gsit" => "Gsit",
            "doze" => "Doze",
            _      => "Pose"
        };

        var style = ImGui.GetStyle();
        float buttonWidth = 70f;
        float rowWidth = (buttonWidth * 3f) + (style.ItemSpacing.X * 2f);

        for (byte i = 0; i <= 8; i++)
        {
            // Center row alignment
            if (i % 3 == 0)
            {
                ImGui.SetCursorPosX((windowWidth - rowWidth) * 0.5f);
            }

            bool isActive = Plugin.IsPoseActive(type, i);
            bool isAvailable = i <= maxPoses;

            // Active button styling
            if (isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFFFFFFFF); 
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF000000); 
            }

            if (!isAvailable) ImGui.BeginDisabled();

            if (ImGui.Button($"{sentenceCaseType} {i}", new Vector2(buttonWidth, 25)))
            {
                if (isAvailable && !isActive) Plugin.ExecuteSyncPose(type, i);
            }

            if (!isAvailable) ImGui.EndDisabled();

            if (isActive) ImGui.PopStyleColor(2);

            if ((i + 1) % 3 != 0 && i != 8)
            {
                ImGui.SameLine();
            }
        }
    }
}