using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP;

[HarmonyPatch]
public class MountedDesignator : Weapon
{
	[SerializeField] private int designatorCount = 1;
	[SerializeField] private float rangeIncrease = 5000f;
	[SerializeField] private MountedTargetDetector[] targetDetectors;
	
	private LaserDesignator designator;
	private bool applied;

	public override void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount weaponMount)
	{
		base.AttachToHardpoint(aircraft, hardpoint, weaponMount);
		designator = aircraft.laserDesignator;
		RemoveDesignators();
		AddDesignators();

		foreach (var detector in targetDetectors)
		{
			detector.AttachToUnit(aircraft, hardpoint.part);
		}
	}

	private void AddDesignators()
	{
		if (applied || designator == null) return;
		applied = true;
		designator.maxTargets += designatorCount;
		designator.range += rangeIncrease;
	}

	private void RemoveDesignators()
	{
		if (!applied || designator == null) return;
		applied = false;
		designator.maxTargets -= designatorCount;
		designator.range -= rangeIncrease;
	}

	private void OnDestroy()
	{
		RemoveDesignators();
	}


	[HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.RegisterWeapon))]
	[HarmonyPrefix]
	private static bool WeaponManager_RegisterWeapon_Prefix(WeaponManager __instance, Weapon weapon, WeaponMount weaponMount, Hardpoint hardpoint)
	{
		if (weapon is not MountedDesignator) return true;
		
		weapon.AttachToHardpoint(__instance.aircraft, hardpoint, weaponMount);
		return false;
	}
}