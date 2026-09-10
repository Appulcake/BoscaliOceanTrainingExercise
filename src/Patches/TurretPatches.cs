using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(Turret))]
public static class TurretPatches
{
	[HarmonyPatch(nameof(Turret.AimTurret), typeof(Vector3))]
	[HarmonyPostfix]
	private static void AimTurret_PostfixVector3(Turret __instance)
	{
		if (!__instance.attachedUnit?.definition.IsShipDefinition() ?? true) return;
		if (!__instance.attachedUnit.LocalSim) return;

		var aimWeapon = __instance.aimSafetyWeapon ?? __instance.GetWeapon();
		
		if (Physics.SphereCast(aimWeapon.transform.position + aimWeapon.transform.forward * 2f, 0.2f, aimWeapon.transform.forward, out _, __instance.attachedUnit?.maxRadius * 2f ?? 200f, -8193))
		{
			foreach (var weapon in __instance.GetComponentsInChildren<Weapon>()) //TODO: Speed up
			{
				weapon.Safety = true;
			}
		}
		else
		{
			foreach (var weapon in __instance.GetComponentsInChildren<Weapon>())
			{
				weapon.Safety = __instance.aimSafetyWeapon != null && __instance.onTarget;
			}
		}
	}
    
	[HarmonyPatch(nameof(Turret.AimTurret), typeof(WeaponStation))]
	[HarmonyPostfix]
	private static void AimTurret_PostfixWeaponStation(Turret __instance)
	{
		if (!__instance.attachedUnit?.definition.IsShipDefinition() ?? true) return;
		if (!__instance.attachedUnit.LocalSim) return;
		
		var aimWeapon = __instance.aimSafetyWeapon ?? __instance.GetWeapon();
		
		var targetDist = __instance.targetRange - (__instance.target.maxRadius + 50f);
		if (Physics.SphereCast(aimWeapon.transform.position + aimWeapon.transform.forward * 2f, 0.2f, aimWeapon.transform.forward, out var hit, __instance.attachedUnit?.maxRadius * 2f ?? 200f, -8193) || (hit.distance < targetDist && hit.distance > 1f))
		{
			foreach (var weapon in __instance.GetComponentsInChildren<Weapon>())
			{
				weapon.Safety = true;
			}
		}
		else
		{
			foreach (var weapon in __instance.GetComponentsInChildren<Weapon>())
			{
				weapon.Safety = __instance.aimSafetyWeapon != null && __instance.onTarget;
			}
		}
	}

	[HarmonyPatch(nameof(Turret.AttachToWeaponManager))]
	[HarmonyPostfix]
	private static void AttachToWeaponManager_Postfix(Turret __instance, Aircraft aircraft)
	{
		if (__instance.targetAcquisitionMode == Turret.TargetAcquisitionMode.parentUnitTargetDetector && __instance.attachedUnit?.radar != null)
		{
			__instance.RegisterTargetDetector(__instance.attachedUnit.radar);
		}
	}
	
	[HarmonyPatch(nameof(Turret.SetTarget), typeof(PersistentID), typeof(byte))]
	[HarmonyPostfix]
	private static void SetTarget_Postfix(Turret __instance, PersistentID id)
	{
		if (__instance.attachedUnit.disabled || !__instance.attachedUnit.definition.IsShipDefinition()) return;
		if (!UnitRegistry.TryGetUnit(id, out var target)) return;

		foreach (var weapon in __instance.weaponStations.SelectMany(w => w.Weapons))
		{
			weapon.SetTarget(target);
		}
		
		__instance.aimSolver.SetTarget(__instance.attachedUnit, target, __instance.aimSafetyWeapon.transform, __instance.aimSafetyWeapon.info);
	}
}