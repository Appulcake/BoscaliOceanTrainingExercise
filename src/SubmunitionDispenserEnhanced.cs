using HarmonyLib;

namespace NOComponentWIP;

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NuclearOption.Networking;
using UnityEngine;


[HarmonyPatch]
public class SubmunitionDispenserEnhanced : SubmunitionDispenser
{
	[SerializeField] private bool targetAir;
	[SerializeField] private bool targetMissile;
	[SerializeField] private bool targetSurface;
	[SerializeField] private bool targetGround;
	[SerializeField] private bool targetBuilding;

	[SerializeField] private float maxSpeed = 2000f;
	[SerializeField] private float minAlignAngle = -1f;
	
	private new void TargetApproachCheck()
	{
		if (!dispensed && !(missile.NetworkHQ == null))
		{
			_ = missile.targetID;
			if (ApproachCheck())
			{
				missile.NetworkHQ.RpcUpdateTrackingInfo(missile.targetID);
				missile.Damage(dmgID, new DamageInfo(0f, 0f, 0f, 1f));
			}
		}
	}

	private bool ApproachCheck()
	{
		if (missile.targetID.TryGetUnit(out var unit) && 
		    missile.NetworkHQ.IsTargetPositionAccurate(unit, detectionRange) && 
		    FastMath.InRange(unit.GlobalPosition(), missile.GlobalPosition(), dispenseDistance) && 
		    unit.LineOfSight(missile.transform.position, 1000f) &&
		    (minAlignAngle == -1f || Vector3.Angle(FastMath.Direction(transform.position, unit.transform.position), missile.transform.forward) <= minAlignAngle))
		{
			return true;
		}

		return false;
	}

	private bool TargetCheck(Unit target)
	{
		if (target is Scenery) return false;
		if (targetMissile && target is Missile) return true;
		if (targetSurface && target is Ship) return true;
		if (targetAir && target is Aircraft) return true;
		if (targetBuilding && target is Building) return true;
		if (targetGround && target is GroundVehicle) return true;
		return false;
	}

	public async new UniTask AssignSubmunitionTargets()
	{
		_ = missile.targetID;
		if (!missile.targetID.TryGetUnit(out var unit))
		{
			return;
		}
		if (detectedUnits == null)
		{
			detectedUnits = new List<Unit>();
		}
		BattlefieldGrid.GetUnitsInRangeNonAlloc(unit.GlobalPosition(), detectionRange, detectedUnits);
		for (int num = detectedUnits.Count - 1; num >= 0; num--)
		{
			Unit unit2 = detectedUnits[num];
			if (unit2.NetworkHQ == missile.NetworkHQ || !TargetCheck(unit2) || unit2.speed > maxSpeed || FastMath.OutOfRange(detectedUnits[num].GlobalPosition(), unit.GlobalPosition(), detectionRange))
			{
				detectedUnits.RemoveAt(num);
			}
		}
		CancellationToken cancel = base.destroyCancellationToken;
		int targetsAssigned = 0;
		int detectedIndex = 0;
		while (targetsAssigned < submunitions.Length && detectedUnits.Count > 0)
		{
			await UniTask.WaitForSeconds(ejectInterval);
			if (cancel.IsCancellationRequested || missile.disabled)
			{
				return;
			}
			if (detectedIndex >= detectedUnits.Count)
			{
				detectedIndex = 0;
			}
			if (detectedUnits[detectedIndex].LineOfSight(missile.transform.position, 1000f))
			{
				if (missile.IsServer)
				{
					Vector3 vector = ((Vector3.Dot(submunitions[targetsAssigned].transform.position - missile.transform.position, missile.transform.right) > 0f) ? missile.transform.right : (-missile.transform.right)) * ejectSpeed;
					NetworkSceneSingleton<Spawner>.i.SpawnMissile(submunitionType.weaponPrefab, submunitions[targetsAssigned].transform.position, missile.transform.rotation, missile.rb.velocity + vector, detectedUnits[detectedIndex], missile);
				}
				submunitions[targetsAssigned].SetActive(value: false);
				targetsAssigned++;
			}
			else
			{
				detectedUnits.RemoveAt(detectedIndex);
			}
			detectedIndex++;
		}
		await UniTask.WaitForSeconds(1);
		if (!cancel.IsCancellationRequested && !missile.disabled && missile.IsServer)
		{
			missile.Networkdisabled = true;
			Object.Destroy(missile.gameObject, 5f);
		}
	}

	[HarmonyPatch(typeof(SubmunitionDispenser), nameof(SubmunitionDispenser.TargetApproachCheck))]
	[HarmonyPrefix]
	private static bool SD_TargetApproachCheck_Prefix(SubmunitionDispenser __instance)
	{
		if (__instance is not SubmunitionDispenserEnhanced enhanced) return true;

		enhanced.TargetApproachCheck();
		
		return false;
	}
	
	
	[HarmonyPatch(typeof(SubmunitionDispenser), nameof(SubmunitionDispenser.AssignSubmunitionTargets))]
	[HarmonyPrefix]
	private static bool SD_AssignSubmunitionTargets_Prefix(SubmunitionDispenser __instance)
	{
		if (__instance is not SubmunitionDispenserEnhanced enhanced) return true;

		enhanced.AssignSubmunitionTargets().Forget();
		
		return false;
	}
	
}
