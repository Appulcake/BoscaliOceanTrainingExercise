using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP.Patches;

[HarmonyPatch(typeof(WeaponStation))]
public static class WeaponStationPatches
{
	[HarmonyPatch(nameof(WeaponStation.LaunchMount))]
	[HarmonyPostfix]
	static void LaunchMount_Postfix(WeaponStation __instance, ref int ___weaponIndex)
	{
		if (__instance.Weapons.Count == 0) return;
        
		if (___weaponIndex >= __instance.Weapons.Count)
		{
			var lastWeapon = __instance.Weapons[__instance.Weapons.Count - 1];
			if (lastWeapon is NetworkMissileLauncher)
			{
				___weaponIndex = 0;
			}
			else return;
		}

		int startIndex = ___weaponIndex;
		int checkedCount = 0;
		int totalWeapons = __instance.Weapons.Count;

		while (IsWeaponEmpty(__instance.Weapons[___weaponIndex]) && checkedCount < totalWeapons)
		{
			___weaponIndex = (___weaponIndex + 1) % totalWeapons;
			checkedCount++;
		}
	}

	[HarmonyPatch(nameof(WeaponStation.UpdateLastFired))]
	[HarmonyPostfix]
	private static void UpdateLastFired_Postfix(WeaponStation __instance, int roundsFired)
	{
		if (__instance.Weapons[0] is NetworkMissileLauncher)
		{
			__instance.Ammo += roundsFired;
		}
	}

	private static bool IsWeaponEmpty(object weapon)
	{
		if (weapon is NetworkMissileLauncher nml)
		{
			return nml.GetAmmoTotal() <= 0 || nml.GetAmmoLoaded() <= 0 || nml.Reloading;
		}
		return false;
	}
	
	// Fix for guns double firing (additional shots being free but real networked entities) on server
	// Comes from a vanilla bug, Aryx' Chimera for example has the same issue (and similar fix)
	private sealed class StationState
	{
		public double LastVolleyTime = double.NegativeInfinity;
	}
	
	private static readonly ConditionalWeakTable<WeaponStation, StationState> States = new();
	private const double VolleyMergeWindow = 0.15;
	
	[HarmonyPatch(typeof(WeaponStation), nameof(WeaponStation.RemoteFireSingle))]
	[HarmonyPrefix]
	private static bool RemoteFireSingle_Prefix(WeaponStation __instance)
	{
		if (__instance == null || __instance.Weapons == null || __instance.Weapons.Count < 2) return true;
		var sequentialGuns = 0;
		foreach (var weapon in __instance.Weapons)
		{
			if (weapon is not SequentialGun) continue;
			sequentialGuns++;
			if (sequentialGuns >= 2) break;
		}
		
		if (sequentialGuns < 2) return true;
		var state = States.GetOrCreateValue(__instance);
		var now = Time.realtimeSinceStartupAsDouble;
		if (now - state.LastVolleyTime < VolleyMergeWindow) return false;
		state.LastVolleyTime = now;
		return true;
	}
}