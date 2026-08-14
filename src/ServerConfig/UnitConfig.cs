using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace NOComponentWIP.ServerConfig;

public class UnitConfigData
{
	public float GlobalCostMultiplier { get; set; } = 1.0f;
	public Dictionary<string, UnitConfigEntry> Units { get; set; } = new Dictionary<string, UnitConfigEntry>();
}

public class UnitConfigEntry
{
	public bool Enabled { get; set; } = true;
	public float? Cost { get; set; } = null;
}

public static class UnitConfig
{
	private static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "BOTE/UnitConfig.jsonc");

	public static UnitConfigData ConfigData { get; set; } = new();

	public static void LoadOrCreateConfig(bool firstInit)
	{
		if (firstInit) return;
		
		string dir = Path.GetDirectoryName(ConfigPath);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

		if (File.Exists(ConfigPath))
		{
			
		}
	}
}