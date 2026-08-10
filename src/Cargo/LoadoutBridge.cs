using System.Collections.Generic;

namespace NOComponentWIP;

public static class LoadoutBridge
{
	private static readonly Dictionary<DeployableUnit, int> manifest = new();

	public static Dictionary<DeployableUnit, int> Manifest => manifest;
	public static bool LoadoutSet { get; private set; }
	public static bool IncludeFOB { get; private set; }
	public static bool BlockInputs { get; set; }

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
	}

	public static void Clear()
	{
		manifest.Clear();
		IncludeFOB = false;
		LoadoutSet = false;
		BlockInputs = false;
	}
}