using HarmonyLib;
using NuclearOption.Jobs;
using UnityEngine;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(AeroPart))]
public class AeroPartPatches
{
	[HarmonyPatch(nameof(AeroPart.ApplyJobFields))]
	[HarmonyPrefix]
	static bool ApplyJobFields_Prefix(AeroPart __instance)
	{
        if (!__instance.parentUnit.definition.IsShipDefinition()) return true;
        
        if (!__instance.JobFields.IsCreated)
        {
            return false;
        }

        ref AeroPartFields reference = ref __instance.JobFields.Ref();

        if (reference.splashed)
        {
            Vector3 position = __instance.xform.position;
            bool flag = false;
            bool flag2 = false;

            if (__instance.parentUnit.speed > 83f && __instance.parentUnit.LocalSim)
            {
                PartJoint[] array = __instance.joints;
                foreach (PartJoint partJoint in array)
                {
                    if (partJoint.joint != null)
                    {
                        partJoint.joint.breakForce = 0f;
                        partJoint.joint.breakTorque = 0f;
                        __instance.attachInfo.attachmentStrength = 0f;
                    }
                }
            }

            if (Physics.Linecast(position + Vector3.up * 100f, position - Vector3.up * 10f, out var hitInfo, PhysicsLayers.StaticsMask))
            {
                flag2 = hitInfo.collider.sharedMaterial == GameAssets.i.WaterMaterial;
                flag = !flag2 && hitInfo.point.y > Datum.LocalSeaY;
            }

            if (!flag2)
            {
                position.y = Datum.LocalSeaY;
            }

            // if (!flag && SceneSingleton<ParticleEffectManager>.i != null)
            // {
            // 	SceneSingleton<ParticleEffectManager>.i.GetPrefabEffect(GameAssets.i.splash_large).Play(position, Quaternion.LookRotation(Vector3.up + new Vector3(rb.velocity.x, 0f, rb.velocity.z) * 0.1f));
            // }
        }

        if (reference.angularDragChanged)
        {
            __instance.rb.angularDrag = reference.angularDrag;
        }

        switch (reference.hasForce)
        {
            case JobForceType.Force:
                __instance.rb.AddForce(reference.force);
                break;
            case JobForceType.ForceAndTorque:
                __instance.rb.AddForce(reference.force);
                __instance.rb.AddTorque(reference.torque);
                break;
        }

        return false;
    }
}