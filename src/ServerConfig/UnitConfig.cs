using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Mirage;
using Newtonsoft.Json;

namespace NOComponentWIP.ServerConfig;

public struct UnitConfigData
{
	public UnitConfigData() { }

	public float GlobalCostMultiplier { get; set; } = 1.0f;
	public Dictionary<string, UnitConfigEntry> Units { get; set; } = new();
}

public struct UnitConfigEntry
{
	public UnitConfigEntry() { }

	public bool Enabled { get; set; } = true;
	public float? Cost { get; set; } = null;

	public int PlayerMax { get; set; } = -1;
	public int FactionMax { get; set; } = -1;
}

public static class UnitConfig
{
	private static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "BOTE/UnitConfig.jsonc");

	public static UnitConfigData LocalConfigData { get; set; } = new();
	public static UnitConfigData ActiveConfigData { get; set; } = new();
	
	public static bool UnitAllowed(string key) => 
		ActiveConfigData.Units.TryGetValue(key, out var u) ? u.Enabled : true;

	public static int PlayerMax(string key) => 
		ActiveConfigData.Units.TryGetValue(key, out var u) ? u.PlayerMax : -1;

	public static int FactionMax(string key) => 
		ActiveConfigData.Units.TryGetValue(key, out var u) ? u.FactionMax : -1;

	public static float UnitCost(string key)
	{
		if (ActiveConfigData.Units.TryGetValue(key, out var unit) && unit.Cost.HasValue)
		{
			return unit.Cost.Value;
		}
		var baseValue = ModAssets.i?.AllDeployableUnits.GetValueOrDefault(key)?.UnitDefinition.value;
		return (baseValue ?? 0f) * ActiveConfigData.GlobalCostMultiplier;
	}

	public static void LoadRemoteConfig(UnitConfigData config)
	{
		ActiveConfigData = config;
	}

	public static void RestoreConfig()
	{
		ActiveConfigData = LocalConfigData;
	}

	public static void LoadOrCreateConfig(bool firstInit)
	{
		if (!firstInit) return;
		
		string dir = Path.GetDirectoryName(ConfigPath);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

		if (File.Exists(ConfigPath))
		{
			try
			{
				string jsonString = File.ReadAllText(ConfigPath);
				LocalConfigData = JsonConvert.DeserializeObject<UnitConfigData>(jsonString);
			}
			catch (Exception ex)
			{
				Plugin.Logger.LogError($"Error loading UnitSettings.jsonc: {ex.Message}");
				LocalConfigData = new UnitConfigData();
			}
		}

		bool modified = false;

		if (ModAssets.i?.AllDeployableUnits != null)
		{
			foreach (var kvp in ModAssets.i.AllDeployableUnits)
			{
				string key = kvp.Key;
				if (!LocalConfigData.Units.ContainsKey(key))
				{
					LocalConfigData.Units[key] = new UnitConfigEntry { Enabled = true, Cost = null };
					modified = true;
				}
			}
		}

		if (modified || !File.Exists(ConfigPath))
		{
			SaveConfig(LocalConfigData);
		}

		ActiveConfigData = LocalConfigData;
	}

	public static void SaveConfig(UnitConfigData config)
	{
		using var sWriter = new StringWriter();
		using (var writer = new JsonTextWriter(sWriter))
		{
			writer.Formatting = Formatting.Indented;
			writer.IndentChar = ' ';
			writer.Indentation = 4;
			
			writer.WriteComment("""
			                    GlobalCostMultiplier: Multiplier on base cost of unit (according to game value data)
			                    Enabled: If false, unit cannot be deployed
			                    Cost: If not null, override cost of unit (in millions)
			                    """);
			writer.WriteRaw("\n");
			
			writer.WriteStartObject();
			
			writer.WritePropertyName("GlobalCostMultiplier");
			writer.WriteValue(config.GlobalCostMultiplier);
			
			writer.WritePropertyName("Units");
			writer.WriteStartObject();

			foreach (var (key, value) in config.Units)
			{
				float baseCost = 0;
				if (ModAssets.i != null && ModAssets.i.AllDeployableUnits.TryGetValue(key, out var unit))
				{
					baseCost = unit.UnitDefinition.value; //millions
				}
				
				writer.WritePropertyName(key);
				writer.WriteStartObject();
				
				writer.WritePropertyName("Enabled");
				writer.WriteValue(value.Enabled);
				
				writer.WritePropertyName("Cost");
				writer.WriteValue(value.Cost);
				writer.WriteRaw(" ");
				writer.WriteComment($"Base Cost: {baseCost}");
				
				writer.WritePropertyName("PlayerMax");
				writer.WriteValue(value.PlayerMax);
				
				writer.WritePropertyName("FactionMax");
				writer.WriteValue(value.FactionMax);
				
				writer.WriteEndObject();
			}
			
			writer.WriteEndObject();
			writer.WriteEndObject();
		}
		
		File.WriteAllText(ConfigPath, sWriter.ToString());
	}
}