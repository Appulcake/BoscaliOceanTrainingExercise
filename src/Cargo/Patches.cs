using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NOComponentWIP;

[HarmonyPatch(typeof(AircraftSelectionMenu))]
public static class AircraftSelectionMenuPatch
{
	private static GameObject uiInstance;
	
	private static Transform newButton;
	
	[HarmonyPatch(nameof(AircraftSelectionMenu.Initialize))]
	[HarmonyPrefix]
	static void Prefix(AircraftSelectionMenu __instance)
	{
		var infoPanel = __instance.transform.Find("LowRow")?.Find("RightPanel")?.Find("InfoPanel");
		if (infoPanel == null) return;
		var container = infoPanel.Find("Container");
		if (container == null) return;
		container.GetComponent<VerticalLayoutGroup>()?.spacing = 5f;
		if (!infoPanel.TryGetComponent<VerticalLayoutGroup>(out var vlg))
		{
			vlg = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
		}
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.padding = new RectOffset(5, 5, 5, 5);
		
		var flyButton = infoPanel.Find("FlyButton")?.GetComponent<Button>();
		if (flyButton == null) return;

		flyButton.onClick.RemoveListener(OnFlyButtonClicked);
		flyButton.onClick.AddListener(OnFlyButtonClicked);
		
		if (flyButton.TryGetComponent<LayoutElement>(out var layoutElement))
		{
			layoutElement.ignoreLayout = false;
		}
		
		if (newButton == null)
		{
			newButton = Object.Instantiate(flyButton.transform, infoPanel);
			newButton.SetSiblingIndex(1);

			var text = newButton.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
			if (text != null) text.text = "Cargo Options >";
			if (text != null) text.enableWordWrapping = false;

			var cargoBtn = newButton.GetComponent<Button>();
			cargoBtn.onClick.RemoveAllListeners();
			cargoBtn.onClick.SetPersistentListenerState(0,  UnityEventCallState.Off);
			cargoBtn.onClick.AddListener(() => SpawnUI(__instance));
		}

		newButton.gameObject.SetActive(false);
	}

	private static void OnFlyButtonClicked()
	{
		if (selected && !LoadoutBridge.LoadoutSet)
		{
			LoadoutBridge.SetLoadout(new(), false);
		}

		if (uiInstance != null)
		{
			var controller = uiInstance.GetComponent<CargoUIController>();
			controller?.Close();
		}
	}
	
	private static bool selected = false;

	[HarmonyPatch(nameof(AircraftSelectionMenu.SpawnPreview))]
	[HarmonyPostfix]
	static void Postfix(AircraftSelectionMenu __instance)
	{
		if (ModAssets.i.ShipDefinitionsWithDeployer.Contains(__instance.previewAircraft?.definition))
		{
			newButton?.gameObject.SetActive(true);
			selected = true;
		}
		else
		{
			newButton?.gameObject.SetActive(false);
			selected = false;
		}
	}

	private static void SpawnUI(AircraftSelectionMenu menu)
	{
		if (uiInstance != null) return;
		
		Canvas rootCanvas = menu.GetComponentInParent<Canvas>();
		if (rootCanvas == null)
		{
			Plugin.Logger.LogError("Could not find a Canvas to spawn the UI on.");
			return;
		}
		
		uiInstance = Object.Instantiate(ModAssets.i.CargoEditorUI, rootCanvas.transform);
		uiInstance.transform.SetAsLastSibling();
		
		var controller = uiInstance.GetComponent<CargoUIController>();
		var manager = menu.previewAircraft?.GetComponent<DeploymentManager>();

		if (controller != null && manager != null)
		{
			controller.Initialize(manager);
		}
		else
		{
			Plugin.Logger.LogError("UI Spawned but CargoUIController or DeploymentManager is missing!");
		}
	}
}

[HarmonyPatch(typeof(PilotPlayerState), nameof(PilotPlayerState.PlayerAxisControls))]
public static class ControlPatch
{
	[HarmonyPrefix]
	private static bool Prefix(PilotPlayerState __instance)
	{
		if (LoadoutBridge.BlockInputs)
		{
			var pps = __instance;
			pps.controlInputs.brake = 0f;
			pps.controlInputs.yaw = 0f;
			pps.controlInputs.pitch = 0f;
			pps.controlInputs.roll = 0f;
			pps.controlInputs.customAxis1 = 0.5f;
			pps.controlInputs.throttle = 0f;
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(Airbase), nameof(Airbase.CanSpawnAircraft))]
public static class CanSpawnAircraftPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Airbase __instance, AircraftDefinition definition, ref bool __result)
	{
		var filter = __instance.GetComponent<AirbaseAIFilter>();
		if (filter == null) return true;
		
		for (int i = 1; i <= 4; i++)
		{
			var frame = new StackFrame(i, false);
			var method = frame.GetMethod();
			if (method != null && method.Name.Contains("FlyAircraftAsync"))
			{
				return true;
			}
		}

		if (filter.CanSpawnAircraft(definition.jsonKey)) return true;

		__result = false;
		return false;
	}
}
