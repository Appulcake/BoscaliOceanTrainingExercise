using HarmonyLib;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(PilotPlayerState))]
public static class PilotPlayerStatePatches
{
	[HarmonyPatch(nameof(PilotPlayerState.PlayerControls))]
	[HarmonyPostfix]
	private static void PlayerControls_Postfix(PilotPlayerState __instance)
	{
		if (!GameManager.flightControlsEnabled || __instance.pilotStrength < 0.2f) return;
		if (!ModAssets.i.ShipDefinitions.Contains(__instance.pilot.aircraft.definition)) return;
		
		var pilot = __instance.pilot;
		var aircraft = pilot.aircraft;
		var player = __instance.player;

		if (player.GetButton("Countermeasures") && !aircraft.countermeasureTrigger)
		{
			aircraft.Countermeasures(true, aircraft.countermeasureManager.activeIndex);
		}

		if (player.GetButtonDown("Gear"))
		{
			if (aircraft.gearState == LandingGear.GearState.LockedExtended)
			{
				aircraft.SetGear(deployed: false);
			}
			else if (aircraft.gearState == LandingGear.GearState.LockedRetracted)
			{
				aircraft.SetGear(deployed: true);
			}
		}
	}
}