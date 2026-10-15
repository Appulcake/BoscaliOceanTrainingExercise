using System.Linq;
using HarmonyLib;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(Gun))]
public static class GunPatches
{
	[HarmonyPatch(nameof(Gun.SpawnBullet))]
	[HarmonyPrefix]
	private static void SpawnBullet_Prefix(Gun __instance)
	{
		if (__instance.bulletSim != null) return;
		var turret =
			__instance.weaponStation.Turrets.
				Where(t => t.GetComponentsInChildren<Gun>().
					Any(g => g == __instance)).
				ToList().
				FirstOrDefault();
		
		if (turret == null) return;
		__instance.bulletSim = BulletSim.Create(__instance.attachedUnit, __instance, turret);
	}
}