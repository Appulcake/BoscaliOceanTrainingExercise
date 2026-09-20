using HarmonyLib;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(TargetCam))]
public static class TargetCamPatches
{
	[HarmonyPrefix]
	[HarmonyPatch(nameof(TargetCam.Update))]
	private static bool Update_Prefix(TargetCam __instance)
	{
		var aircraft = __instance.aircraft;
		var player = __instance.aircraft?.Player;
		
		if (aircraft == null || player == null || !player.IsLocalPlayer) return false;

		return true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(TargetCam.Initialize))]
	private static bool Initialize_Prefix(TargetCam __instance)
	{
		if (__instance.aircraft.Player == null) return false;
		return true;
	}
}