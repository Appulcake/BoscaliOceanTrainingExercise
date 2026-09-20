using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NuclearOption.NetworkTransforms;

namespace BoscaliOceanTrainingExercise.Patches;

[HarmonyPatch(typeof(AircraftNetworkTransform))]
public static class AircraftNetworkTransformPatches
{
	private static FieldInfo NetworkSnapshot_globalPos = AccessTools.Field(typeof(NetworkTransformBase.NetworkSnapshot), nameof(NetworkTransformBase.NetworkSnapshot.globalPos));
	private static FieldInfo GlobalPosition_y = AccessTools.Field(typeof(GlobalPosition), nameof(GlobalPosition.y));
	
	[HarmonyPatch(nameof(AircraftNetworkTransform.ProcessNextClientAuth))]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> ProcessNextClientAuth_Transpiler(
		IEnumerable<CodeInstruction> instructions)
	{
		var matcher = new CodeMatcher(instructions);

		matcher.MatchForward(true,
			new CodeMatch(OpCodes.Ldfld, NetworkSnapshot_globalPos),
			new CodeMatch(OpCodes.Ldfld, GlobalPosition_y),
			new CodeMatch(OpCodes.Ldc_R4, -10f));

		if (!matcher.IsValid)
		{
			Plugin.Logger.LogError("Failed to find match in AircraftNetworkTransform.ProcessNextClientAuth");
			return matcher.InstructionEnumeration();
		}

		matcher.Set(OpCodes.Ldc_R4, float.NegativeInfinity);
		
		return matcher.InstructionEnumeration();
	}
}