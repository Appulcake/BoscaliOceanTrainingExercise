using System;
using HarmonyLib;

namespace NOComponentWIP.Patches;

[HarmonyPatch(typeof(Airbase))]
public static class FOBAirbasePatches
{
    // Send RPC to current clients when a FOB is fully destroyed (center gone)
    [HarmonyPatch(nameof(Airbase.OnDestroy))]
    [HarmonyPrefix]
    private static void OnDestroy_Prefix(Airbase __instance)
    {
        var saved = __instance?.SavedAirbase;
        if (saved == null || string.IsNullOrEmpty(saved.UniqueName) ||
            !saved.UniqueName.StartsWith("FOB_", StringComparison.Ordinal))
        {
            return;
        }
        
        var mission = MissionManager.CurrentMission;
        mission?.airbases?.Remove(saved);
        
        if (saved.Airbase == __instance)
            saved.Airbase = null;
    }
}