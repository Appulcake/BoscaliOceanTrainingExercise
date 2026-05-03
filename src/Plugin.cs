using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine;

namespace NOComponentWIP;

[BepInPlugin("NOComponentsWIP", "NOComponentsWIP", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private void Awake()
	{
		Logger = base.Logger;
		Harmony harmony = new Harmony("NOComponentWIP");
		harmony.PatchAll();
		Logger.LogInfo("Boscali Ocean Training Exercise Loaded");
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			if (AircraftSwitcher.i == null) return;
			if (!GameManager.GetLocalPlayer(out NuclearOption.Networking.Player player)) return;
			if (!GameManager.GetLocalAircraft(out var aircraft)) return;
			if (aircraft.weaponManager.targetList.Count == 0) return;
			var targetUnit = aircraft.weaponManager.targetList[0];
			if (targetUnit == null || targetUnit is not Aircraft newAircraft) return;
			AircraftSwitcher.i.SwitchAircraft(player, aircraft, newAircraft);
		}
	}
}

[HarmonyPatch]
public static class Mod_Input
{
	private static List<int> catIDs = new List<int>();
	private static int newMapID = -1;
	
	private static List<string> customActions =
	[
		"Menu",
		"Deploy Unit",
		"Next Unit",
		"Previous Unit",
		"Call Resupply",
		"Call Resupply - Player",
		"Select/Deselect FOB",
		"Control UI - Select",
		"Next Camera",
		"Previous Camera"
	];
	
	[HarmonyPatch(typeof(InputManager_Base), nameof(InputManager_Base.Awake))]
	[HarmonyPrefix]
	private static void Prefix(InputManager_Base __instance)
	{
		SetupActions(__instance);
	}
	
	private static void SetupActions(InputManager_Base manager)
	{
		var actions = manager?.userData?.actions;
		if (actions == null) return;
		var categories = manager?.userData?.actionCategories;
		if (categories == null) return;
		var mapCategories = manager?.userData?.mapCategories;
		if (mapCategories  == null) return;
		var newCat = new InputCategory
		{
			descriptiveName = "Boscali Ocean Training Exercise",
			id = GetNewCategoryID(categories),
			name = "Boscali Ocean Training Exercise",
			userAssignable = true
		};
		manager.userData.actionCategories.Add(newCat);
		manager.userData.actionCategoryMap.AddCategory(newCat.id);

		foreach (var action in customActions)
		{
			var newAction = new InputAction()
			{
				id = GetNewActionID(actions),
				name = action,
				type = InputActionType.Button,
				descriptiveName = action,
				categoryId = newCat.id,
				userAssignable = true
			};
			actions.Add(newAction);
			manager.userData.actionCategoryMap.AddAction(newCat.id, newAction.id);
		}
		catIDs.Add(newCat.id);

		var newMapCat = new InputMapCategory();
		newMapCat._name = "BOTE";
		newMapCat.id = GetNewMapCatID(mapCategories);
		newMapCat.descriptiveName = "BOTE";
		newMapCat.userAssignable = true;
		newMapCat.checkConflictsWithAllCategories = true;
		mapCategories.Add(newMapCat);
		newMapID = newMapCat.id;

	}

	[HarmonyPatch(typeof(ControlMapper), nameof(ControlMapper.Initialize))]
	private static void Prefix(ControlMapper __instance)
	{
		var newMappingSet = new ControlMapper.MappingSet(newMapID, ControlMapper.MappingSet.ActionListMode.ActionCategory, catIDs.ToArray(), []);
		__instance._mappingSets = __instance._mappingSets.AddToArray(newMappingSet);
		GameManager.playerInput.controllers.maps.SetMapsEnabled(true, "BOTE");

	}

	private static int GetNewSetID(List<ControlMapper.MappingSet> categories)
	{
		if (categories == null || !categories.Any()) return 1;
		return categories.Max(c => c.mapCategoryId) + 1;
	}
	
	private static int GetNewCategoryID(List<InputCategory> categories)
	{
		if (categories == null || !categories.Any()) return 1;
		return categories.Max(c => c.id) + 1;
	}

	private static int GetNewMapCatID(List<InputMapCategory> categories)
	{
		if (categories == null || !categories.Any()) return 1;
		return categories.Max(c => c.id) + 1;
	}
	
	private static int GetNewActionID(List<InputAction> actions)
	{
		if (actions == null || !actions.Any()) return 1;
		return actions.Max(a => a.id) + 1;
	}
}