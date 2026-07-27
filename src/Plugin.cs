using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine;

namespace NOComponentWIP;

public static class MyPluginInfo
{
	public const string PLUGIN_GUID = "com.minec.bote";
	public const string PLUGIN_NAME = "BoscaliOceanTrainingExercise";
	public const string PLUGIN_VERSION = "1.5.0";
}


[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	internal static Plugin Instance;
	private void Awake()
	{
		Instance = this;
		Logger = base.Logger;
		Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		harmony.PatchAll();
		Logger.LogInfo("Boscali Ocean Training Exercise Loaded");

		autoResetBind = Config.Bind($"{MyPluginInfo.PLUGIN_NAME}",
			"Radial Menu AutoReset",
			true,
			new ConfigDescription($"Radial menu for BOTE auto reset to main radial menu."));
	}

	private void Update()
	{
		RadialMenu.Update();
		
		if (GameManager.gameState != GameState.SinglePlayer) return;
		if (Input.GetKeyDown(KeyCode.Semicolon))
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

	private ConfigEntry<bool> autoResetBind;
	public bool AutoResetBind => autoResetBind.Value;
}