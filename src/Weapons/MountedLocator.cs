using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP;

[HarmonyPatch]
public class MountedLocator : Weapon
{
	[SerializeField] private bool onlySurface;
	private float rewardAmount;
	private float rewardCount;
	private float rewardThreshold = 1f;
	
	private Aircraft aircraft;
	
	public override void AttachToHardpoint(Aircraft aircraft, Hardpoint hardpoint, WeaponMount weaponMount)
	{
		base.AttachToHardpoint(aircraft, hardpoint, weaponMount);
		this.aircraft = aircraft;
		aircraft.onRadarWarning += RadarLocator_OnRadarWarning;
		hardpoint.part.onParentDetached += MountedLocator_OnPartDetached;
	}

	private void RadarLocator_OnRadarWarning(Aircraft.OnRadarWarning radarWarning)
	{
		if (!aircraft.IsServer || !(aircraft.NetworkHQ != null))
		{
			return;
		}
		if (onlySurface)
		{
			TypeIdentity typeIdentity = radarWarning.emitter.definition.typeIdentity;
			if (typeIdentity.air > 0f || typeIdentity.missile > 0f)
			{
				return;
			}
		}
		if (aircraft.Player != null && radarWarning.emitter.NetworkHQ != null && radarWarning.emitter.NetworkHQ != aircraft.NetworkHQ)
		{
			float num = 0f;
			if (!aircraft.NetworkHQ.trackingDatabase.ContainsKey(radarWarning.emitter.persistentID))
			{
				num = 0.01f;
			}
			else if (!aircraft.NetworkHQ.IsTargetPositionAccurate(radarWarning.emitter, 500f))
			{
				num = 0.005f;
			}
			rewardCount += num * Mathf.Sqrt(radarWarning.emitter.definition.value);
			rewardAmount += num * Mathf.Sqrt(radarWarning.emitter.definition.value);
			if (rewardCount > rewardThreshold)
			{
				aircraft.NetworkHQ.ReportReconAction(aircraft.Player, rewardAmount);
				rewardAmount = 0f;
				rewardCount = 0f;
			}
		}
		aircraft.NetworkHQ.CmdUpdateTrackingInfo(radarWarning.emitter.persistentID);
	}
	
	private void MountedLocator_OnPartDetached(UnitPart part) => aircraft?.onRadarWarning -= RadarLocator_OnRadarWarning;
	
	[HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.RegisterWeapon))]
	[HarmonyPrefix]
	private static bool WeaponManager_RegisterWeapon_Prefix(WeaponManager __instance, Weapon weapon, WeaponMount weaponMount, Hardpoint hardpoint)
	{
		if (weapon is not MountedLocator) return true;
		
		weapon.AttachToHardpoint(__instance.aircraft, hardpoint, weaponMount);
		return false;
	}
}