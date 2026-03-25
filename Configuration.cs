using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace Poser;

[Serializable]
public class CategoryConfig
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    
    public bool LockWindow { get; set; } = false;
    public float? WindowOpacity { get; set; } = null;

    // Default category states
    public List<CategoryConfig> Categories { get; set; } = new()
    {
        new CategoryConfig { Type = "idle", Label = "Idle Poses", Enabled = true },
        new CategoryConfig { Type = "sit",  Label = "Sit Poses",  Enabled = true },
        new CategoryConfig { Type = "gsit", Label = "Ground Sit Poses", Enabled = true },
        new CategoryConfig { Type = "doze", Label = "Doze Poses", Enabled = true }
    };

    [NonSerialized]
    private IDalamudPluginInterface? PluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}