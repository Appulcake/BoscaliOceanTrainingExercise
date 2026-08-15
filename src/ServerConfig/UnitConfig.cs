using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace NOComponentWIP.ServerConfig;

public class UnitConfigData
{
	public float GlobalCostMultiplier { get; set; } = 1.0f;
	public Dictionary<string, UnitConfigEntry> Units { get; set; } = new();
}

public class UnitConfigEntry
{
	public bool Enabled { get; set; } = true;
	public float? Cost { get; set; } = null;

	public int PlayerMax { get; set; } = -1;
	public int FactionMax { get; set; } = -1;
}

public static class UnitConfig
{
	private static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "BOTE/UnitConfig.jsonc");

	public static UnitConfigData ConfigData { get; set; } = new();
	
	public static bool UnitAllowed(string key) => ConfigData.Units[key].Enabled;
	public static int PlayerMax(string key) => ConfigData.Units[key].PlayerMax;
	public static int FactionMax(string key) => ConfigData.Units[key].FactionMax;
	public static float UnitCost(string key)
	{
		var unit =  ConfigData.Units[key];
		if (unit.Cost == null) return ModAssets.i?.AllDeployableUnits[key].UnitDefinition.value ?? 0f;
		return unit.Cost ?? 0f;
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
				ConfigData = JsonConvert.DeserializeObject<UnitConfigData>(jsonString);
			}
			catch (Exception ex)
			{
				Plugin.Logger.LogError($"Error loading UnitSettings.jsonc: {ex.Message}");
				ConfigData = new UnitConfigData();
			}
		}

		bool modified = false;

		if (ModAssets.i?.AllDeployableUnits != null)
		{
			foreach (var kvp in ModAssets.i.AllDeployableUnits)
			{
				string key = kvp.Key;
				if (!ConfigData.Units.ContainsKey(key))
				{
					ConfigData.Units[key] = new UnitConfigEntry { Enabled = true, Cost = null };
					modified = true;
				}
			}
		}

		if (modified || !File.Exists(ConfigPath))
		{
			SaveConfig(ConfigData);
		}
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