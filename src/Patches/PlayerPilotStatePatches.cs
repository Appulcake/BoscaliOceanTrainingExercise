using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP.Patches;

[HarmonyPatch(typeof(PilotPlayerState))]
public static class PilotPlayerStatePatches
{
	[HarmonyPatch(nameof(PilotPlayerState.PlayerControls))]
	[HarmonyPostfix]
	private static void PlayerControls_Postfix(PilotPlayerState __instance)
	{
		if (!GameManager.flightControlsEnabled || __instance.pilotStrength < 0.2f) return;
		if (!ModAssets.i.ShipDefinitions.Contains(__instance.pilot.aircraft.definition)) return;

		if (__instance.player.GetButton("Countermeasures") && !__instance.pilot.aircraft.countermeasureTrigger)
		{
			__instance.pilot.aircraft.Countermeasures(true, __instance.pilot.aircraft.countermeasureManager.activeIndex);
		}

		if (__instance.player.GetButtonDown("Gear"))
		{
			if (__instance.pilot.aircraft.gearState == LandingGear.GearState.LockedExtended)
			{
				__instance.pilot.aircraft.SetGear(deployed: false);
			}
			else if (__instance.pilot.aircraft.gearState == LandingGear.GearState.LockedRetracted)
			{
				__instance.pilot.aircraft.SetGear(deployed: true);
			}
		}
	}
	
	[HarmonyPatch(typeof(PilotPlayerState), nameof(PilotPlayerState.UpdateState))]
	[HarmonyPrefix]
	private static bool UpdateState_Prefix(Pilot pilot)
	{
		var aircraft = pilot?.aircraft;
		
		if (aircraft == null || !GameManager.IsLocalAircraft(aircraft) || !aircraft.TryGetShipBridge(out var bridge)
		    || !GameManager.playerInput.GetButtonDown("Eject"))
			return true;
		
		if (!bridge.CombatDisembarkLocked)
		{
			bridge.ClearDisembarkConfirmation();
			return true;
		}
		
		if (bridge.ConfirmDisembarkConfirmation())
			return true;
		
		bridge.ArmDisembarkConfirmation();
		var remaining = bridge.CombatDisembarkRemaining;
		var totalSeconds = Mathf.CeilToInt(remaining);
		var minutes = totalSeconds / 60;
		var seconds = totalSeconds % 60;
		
		SceneSingleton<AircraftActionsReport>.i.ReportText(
			$"<b>COMBAT TIMER ACTIVE ({minutes}:{seconds:00})</b>\n" +
			"Disembarking now will not return ship to inventory!\n" +
			"Press eject again within 10 seconds to confirm.", 9.5f);
		
		return false;
	}
}