using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace Poser;

public class ConfigWindow : Window, IDisposable
{
    private Poser Plugin { get; init; }

    public ConfigWindow(Poser plugin) : base(
        "Poser Configuration",
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        Plugin = plugin;
        AllowPinning = false;
        AllowClickthrough = false;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var config = Plugin.Configuration;

        // Window lock toggle
        bool lockWindow = config.LockWindow;
        if (ImGui.Checkbox("Lock Window", ref lockWindow))
        {
            config.LockWindow = lockWindow;
            config.Save();
        }

        ImGui.Spacing();

        // Opacity slider and reset
        float opacity = Plugin.Window.BgAlpha ?? 1.0f;
        ImGui.SetNextItemWidth(125f); 
        if (ImGui.SliderFloat("Opacity", ref opacity, 0.0f, 1.0f, "%.2f"))
        {
            Plugin.Window.BgAlpha = opacity;
            config.WindowOpacity = opacity;
            config.Save();
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("Reset##Opacity"))
        {
            Plugin.Window.BgAlpha = null;
            config.WindowOpacity = null;
            config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.Text("Category Order & Visibility");
        ImGui.Spacing();

        // Category toggles and reordering
        for (int i = 0; i < config.Categories.Count; i++)
        {
            var cat = config.Categories[i];
            
            ImGui.PushID(cat.Type);
            
            bool enabled = cat.Enabled;
            if (ImGui.Checkbox("##hide", ref enabled))
            {
                cat.Enabled = enabled;
                config.Save();
            }
            
            ImGui.SameLine();
            
            ImGui.BeginDisabled(i == 0);
            if (ImGui.Button("↑"))
            {
                var temp = config.Categories[i - 1];
                config.Categories[i - 1] = cat;
                config.Categories[i] = temp;
                config.Save();
            }
            ImGui.EndDisabled();

            ImGui.SameLine();

            ImGui.BeginDisabled(i == config.Categories.Count - 1);
            if (ImGui.Button("↓"))
            {
                var temp = config.Categories[i + 1];
                config.Categories[i + 1] = cat;
                config.Categories[i] = temp;
                config.Save();
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.Text(cat.Label);
            
            ImGui.PopID();
        }
    }
}