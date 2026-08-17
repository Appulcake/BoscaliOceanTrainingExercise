using System;
using System.Collections.Generic;
using NOComponentWIP.ServerConfig;

namespace NOComponentWIP;

public static class LoadoutBridge
{
	private static readonly Dictionary<DeployableUnit, int> manifest = new();

	public static Dictionary<DeployableUnit, int> Manifest => manifest;
	public static bool LoadoutSet { get; private set; }
	public static bool IncludeFOB { get; private set; }
	public static bool BlockInputs { get; set; }

	public static Action onLoadoutChange;

	public static void SetLoadout(Dictionary<DeployableUnit, int> sourceManifest, bool includeFob)
	{
		manifest.Clear();

		if (sourceManifest != null)
		{
			foreach (var kvp in sourceManifest)
			{
				if (kvp.Key != null && kvp.Value > 0)
				{
					manifest[kvp.Key] = kvp.Value;
				}
			}
		}

		IncludeFOB = includeFob;
		LoadoutSet = true;
		onLoadoutChange?.Invoke();
	}

	public static void Clear()
	{
		manifest.Clear();
		IncludeFOB = false;
		LoadoutSet = false;
		BlockInputs = false;
		onLoadoutChange?.Invoke();
	}
	
	public static float CalculateCost(Dictionary<DeployableUnit, int> manifest)
	{
		float cost = 0f;
        
		foreach (var unit in manifest)
		{
			cost += UnitConfig.UnitCost(unit.Key.JsonKey) * unit.Value;
		}

		return cost;
	}
}