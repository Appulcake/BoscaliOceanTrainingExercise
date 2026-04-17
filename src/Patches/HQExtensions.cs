using System.Linq;
using Mirage;
using UnityEngine;

namespace NOComponentWIP.Patches;

public static class HQExtensions
{
	public static bool GetNearestAircraftCapableAirbase(this FactionHQ hq, Vector3 position, AircraftDefinition[] definitions, out Airbase validAirbase)
	{
		validAirbase = null;
		if (hq == null) return false;
		
		var sortedBases = hq.airbasesUnsorted
			.Select(item => item.Value)
			.Where(ab => ab != null && !ab.disabled && ab.CurrentHQ == hq)
			.OrderBy(ab => Vector3.Distance(position, ab.transform.position));

		foreach (Airbase airbase in sortedBases)
		{
			foreach (var hangar in airbase.hangars)
			{
				if (hangar != null && !hangar.Disabled && hangar.availableAircraft.Any(definitions.Contains))
				{
					validAirbase = airbase;
					return true; 
				}
			}
		}

		return false;
	}

	public static bool AnyNearAirbaseInRange(this FactionHQ hq, Vector3 fromPosition, out Airbase airbase, float range = 1000, Airbase excludeAirbase = null)
	{
		airbase = null;
		foreach (NetworkBehaviorSyncvar<Airbase> item in hq.airbasesUnsorted)
		{
			Airbase ab = item.Value;
			if (ab != null)
			{
				if (excludeAirbase == ab) continue;
				if (FastMath.InRange(fromPosition, ab.center.position, range))
				{
					airbase = ab;
					return true;
				}
			}
		}

		return false;
	}
}