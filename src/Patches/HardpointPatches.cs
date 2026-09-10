using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(Hardpoint))]
public class HardpointPatches
{
	[HarmonyPatch(nameof(Hardpoint.SpawnMount))]
	[HarmonyPostfix]
	private static void SpawnMount_Postfix(Aircraft aircraft, WeaponMount weaponMount, GameObject __result)
	{
		foreach (var water in __result.GetComponentsInChildren<WaterEffect>())
		{
			water.unit = aircraft;
		}
		
		if (!weaponMount.turret) return;
		foreach (var turret in __result.GetComponentsInChildren<Turret>().Skip(1))
		{
			turret.AttachToWeaponManager(aircraft);
		}
	}
}