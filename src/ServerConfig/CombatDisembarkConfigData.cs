using System;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace NOComponentWIP.ServerConfig;

public class CombatDisembarkConfigData
{
    public bool CombatTimerEnabled { get; set; } = false;
    public float CruiseMissileTimer { get; set; } = 300f;
    public float BallisticMissileTimer { get; set; } = 300f;
    public float OpticalCombatTimer { get; set; } = 300f;
}

public static class CombatDisembarkConfig
{
    private static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "BOTE/CombatDisembark.jsonc");
    public static CombatDisembarkConfigData Active { get; private set; } = new();
    
    public static void LoadOrCreateConfig()
    {
        var dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                Active = JsonConvert.DeserializeObject<CombatDisembarkConfigData>(json)
                         ?? new CombatDisembarkConfigData();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading CombatDisembark.jsonc: {ex.Message}");
                Active = new CombatDisembarkConfigData();
            }
        }
        else
        {
            Active = new CombatDisembarkConfigData();
            SaveConfig();
        }
        
        CombatDisembark.ClearWeaponCache();
    }
    
    // Unused, placeholder in case this should be live reloadable somewhere
    public static void ReloadConfig()
    {
        LoadOrCreateConfig();
    }
    
    private static void SaveConfig()
    {
        const string header = """
        /*
        --- Combat Disembark ---
        System that allows a server/host to set a combat timer upon launching certain missile types from ships, until that timer expires disembarking in any way won't return that ship to that player's inventory.

        This can help combat this gameplay loop where someone spawns, sends all their anti ground missiles without leaving dock, safe disembarks to fully rearm, and repeat.

        Only server/host needs to enable this and is authoritative, clients see on their UI what their current timer is and during that they need to double press eject within 10 seconds to confirm.

        CombatTimerEnabled: Set this to true to enable combat timer system
        CruiseMissileTimer: Combat timer (in seconds) after launching cruise missiles
        BallisticMissileTimer: Combat timer (in seconds) after launching ballistic missiles
        OpticalCombatTimer: Combat timer (in seconds) after launching optical missiles
        */
        """;
        
        var json = JsonConvert.SerializeObject(Active, Formatting.Indented);
        File.WriteAllText(ConfigPath, header + Environment.NewLine + json);
    }
}