using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP.Patches;

[HarmonyPatch(typeof(ShipPart))]
public class ShipPartPatches
{
    [HarmonyPatch("Leak")]
    [HarmonyPrefix]
    static bool Leak(ShipPart __instance)
    {
        if (__instance.GetType() != typeof(AircraftShipPart))
        {
            return true;
        }
        var bridge = __instance.parentUnit?.GetComponent<ShipPartBridge>();
        if (bridge == null) return true;
        
        if (__instance.transform.position.y - __instance.height < Datum.SeaLevel.y) 
        {
            __instance.displacement -= __instance.leakRate * Time.deltaTime;
        }
        
        if (__instance.compartmentalized || !(__instance.displacement < __instance.originalDisplacement * bridge.damageControlDeploymentThreshold))
        {
            return false;
        }

        bridge.damageControlAvailable -= __instance.originalDisplacement;
        __instance.compartmentalized = true;
        
        if (bridge.damageControlAvailable <= 0f)
        {
            foreach (var part in __instance.connectedCompartments)
            {
                part.Flood();
            }
        }
        return false;
    }

    [HarmonyPatch("DamageControl")]
    [HarmonyPrefix]
    static bool DamageControl(ShipPart __instance)
    {
        if (__instance == null)  return true;
        if (__instance.GetType() != typeof(AircraftShipPart))
        {
            return true;
        }
        var bridge = __instance.parentUnit?.GetComponent<ShipPartBridge>();
        if (bridge == null) return true;
        
        if (!__instance.compartmentalized && !__instance.detachedFromUnit && !__instance.parentUnit.disabled && !__instance.submerged && !(bridge.damageControlAvailable <= 0f))
        {
            float rate = 0.02f * __instance.leakRateMin;
            __instance.leakRate -= rate;
            __instance.leakRate = Mathf.Max(__instance.leakRate, 0f);
            float rate2 = 0.001f * __instance.originalDisplacement;
            __instance.displacement += rate2;
            __instance.displacement = Mathf.Min(__instance.displacement, __instance.originalDisplacement);
            bridge.damageControlAvailable -= 10f * rate + rate2;
        }

        return false;
    }
}