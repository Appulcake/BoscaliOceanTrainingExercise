using System.Collections.Generic;
using HarmonyLib;

namespace BoscaliOceanTrainingExercise.Patches;

/*[HarmonyPatch(typeof(WeaponManager))]
public class WeaponManagerPatches
{
	[HarmonyPatch(nameof(WeaponManager.SetActiveStation))]
	[HarmonyPostfix]
	private static void SetActiveStation_Postfix(WeaponManager __instance)
	{
		Aircraft aircraft = __instance.aircraft;
		if (aircraft?.Player == null) return;

		if (!aircraft.definition.IsShipDefinition()) return;

		foreach (var weapon in __instance.currentWeaponStation?.Weapons ?? new List<Weapon>())
		{
			Turret turret = weapon.GetComponentInParent<Turret>();
			turret.SetManual(true);
		}
	}
}*/