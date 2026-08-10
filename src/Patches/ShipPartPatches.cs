using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP.Patches;

[HarmonyPatch(typeof(ShipPart))]
public class ShipPartPatches
{
    [HarmonyPatch(nameof(ShipPart.Leak))]
    [HarmonyPrefix]
    private static bool Leak(ShipPart __instance)
    {
        if (__instance is not AircraftShipPart asp)
        {
            return true;
        }
        asp.Leak();
        return false;
    }

    [HarmonyPatch(nameof(ShipPart.DamageControl))]
    [HarmonyPrefix]
    private static bool DamageControl(ShipPart __instance)
    {
        if (__instance is not AircraftShipPart asp)
        {
            return true;
        }
        asp.DamageControl();
        return false;
    }
}