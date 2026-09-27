using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(Turret))]
public static class TurretPatches
{
	private static Dictionary<Turret, Weapon[]> turretWeaponLookup = new();

	private static void RegisterTurret(Turret turret)
	{
		turretWeaponLookup.Add(turret, turret.GetComponentsInChildren<Weapon>());
	}

	private static void UnregisterTurret(Turret turret)
	{
		turretWeaponLookup.Remove(turret);
	}

	[HarmonyPatch(nameof(Turret.Awake))]
	[HarmonyPostfix]
	private static void Awake_Postfix(Turret __instance)
	{
		RegisterTurret(__instance);
	}

	[HarmonyPatch(nameof(Turret.OnDestroy))]
	[HarmonyPrefix]
	private static void OnDestroy_Prefix(Turret __instance)
	{
		UnregisterTurret(__instance);
	}
	
	[HarmonyPatch(nameof(Turret.AimTurret), typeof(Vector3))]
	[HarmonyPostfix]
	private static void AimTurret_PostfixVector3(Turret __instance)
	{
		var attachedUnit = __instance?.attachedUnit;
		
		if (!attachedUnit?.definition.IsShipDefinition() ?? true) return;
		if (!attachedUnit.LocalSim) return;

		var aimWeapon = __instance.aimSafetyWeapon ?? __instance.GetComponentInChildren<Weapon>();
		if (aimWeapon == null) return; 
		var aimWeaponTransform = aimWeapon.transform;
		var aimWeaponPosition = aimWeaponTransform.position;
		var aimWeaponForward = aimWeapon.transform.forward;
		
		if (Physics.SphereCast(aimWeaponPosition + aimWeaponForward * 2f, 0.2f, aimWeaponForward, out _, attachedUnit.maxRadius * 2f, -8193))
		{
			if (!turretWeaponLookup.TryGetValue(__instance, out Weapon[] weapons)) return;
			foreach (var weapon in weapons)
			{
				weapon.Safety = true;
			}

		}
		else
		{
			if (!turretWeaponLookup.TryGetValue(__instance, out Weapon[] weapons)) return;
			foreach (var weapon in weapons)
			{
				weapon.Safety = __instance.aimSafetyWeapon != null && !__instance.onTarget;
			}
		}
	}
    
	[HarmonyPatch(nameof(Turret.AimTurret), typeof(WeaponStation))]
	[HarmonyPostfix]
	private static void AimTurret_PostfixWeaponStation(Turret __instance)
	{
		var attachedUnit = __instance?.attachedUnit;
		
		if (!attachedUnit?.definition.IsShipDefinition() ?? true) return;
		if (!attachedUnit.LocalSim) return;
		
		var aimWeapon = __instance.aimSafetyWeapon ?? __instance.GetComponentInChildren<Weapon>();
		if (aimWeapon == null) return; 
		var aimWeaponTransform = aimWeapon.transform;
		var aimWeaponPosition = aimWeaponTransform.position;
		var aimWeaponForward = aimWeapon.transform.forward;
		
		var targetDist = __instance.targetRange - (__instance.target?.maxRadius + 50f) ?? 100f;
		
		if (Physics.SphereCast(aimWeaponPosition + aimWeaponForward * 2f, 0.2f, aimWeaponForward, out var hit, attachedUnit.maxRadius * 2f, -8193) || (hit.distance < targetDist && hit.distance > 1f))
		{
			if (!turretWeaponLookup.TryGetValue(__instance, out Weapon[] weapons)) return;
			foreach (var weapon in weapons)
			{
				weapon?.Safety = true;
			}

		}
		else
		{
			if (!turretWeaponLookup.TryGetValue(__instance, out Weapon[] weapons)) return;
			foreach (var weapon in weapons)
			{
				weapon?.Safety = __instance.aimSafetyWeapon != null && !__instance.onTarget;
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
		
		if (__instance.weaponStations.Length <= 1) return;

		List<WeaponInfo> infos = new List<WeaponInfo>();

		for (int i = 1; i < __instance.weaponStations.Length; i++)
		{
			infos.Add(__instance.weaponStations[i].WeaponInfo);
		}
		
		foreach (WeaponStation weaponStation in aircraft.weaponStations)
		{
			if (infos.Contains(weaponStation.WeaponInfo))
			{
				weaponStation.AssignTurret(__instance);
			}
		}
	}
	
	[HarmonyPatch(nameof(Turret.SetTarget), typeof(PersistentID), typeof(byte))]
	[HarmonyPostfix]
	private static void SetTarget_Postfix(Turret __instance, PersistentID id)
	{
		if (__instance.attachedUnit.disabled || !__instance.attachedUnit.definition.IsShipDefinition()) return;
		if (!UnitRegistry.TryGetUnit(id, out var target)) return;

		foreach (var weapon in __instance.GetComponentsInChildren<Weapon>())
		{
			weapon?.SetTarget(target);
		}

		var aimWeapon = __instance.aimSafetyWeapon ?? __instance.GetComponentInChildren<Weapon>();
		
		__instance.aimSolver.SetTarget(__instance.attachedUnit, target, aimWeapon.transform, aimWeapon.info);
	}
}