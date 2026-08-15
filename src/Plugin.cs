using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NOComponentWIP.ServerConfig;
using Rewired;
using Rewired.UI.ControlMapper;
using UnityEngine;

namespace NOComponentWIP;

public static class MyPluginInfo
{
	public const string PLUGIN_GUID = "com.minec.bote";
	public const string PLUGIN_NAME = "BoscaliOceanTrainingExercise";
	public const string PLUGIN_VERSION = "1.5.1";
}


[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.nikkorap.blueprinter")]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	internal static Plugin Instance;
	
	// ----- CONFIG -----
	
	private ConfigEntry<bool> menuAutoReset;
	public bool MenuAutoReset => menuAutoReset.Value;

	private ConfigEntry<bool> enableUnitEconomy;
	public bool EnableUnitEconomy => enableUnitEconomy.Value;

	private ConfigEntry<bool> enableUnitLimits;
	public bool EnableUnitLimits => enableUnitLimits.Value;

	private void SetupConfig()
	{
		menuAutoReset = Config.Bind($"{MyPluginInfo.PLUGIN_NAME}",
			"Radial Menu AutoReset",
			true,
			new ConfigDescription($"Auto reset to main radial menu."));
		
		enableUnitEconomy = Config.Bind($"{MyPluginInfo.PLUGIN_NAME}",
			"Enable Unit Economy",
			false,
			new ConfigDescription($"Enable unit allocation costs for players."));
		
		enableUnitLimits = Config.Bind($"{MyPluginInfo.PLUGIN_NAME}",
			"Enable Unit Limits",
			false,
			new ConfigDescription($"Enable unit limits for players/faction."));
	}
	
	private void Awake()
	{
		Instance = this;
		Logger = base.Logger;
		InitializeMirageReaderWriters(typeof(Plugin).Assembly);
		Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		harmony.PatchAll();
		SetupConfig();

		ModAssets.OnInitialize += UnitConfig.LoadOrCreateConfig;
		
		Logger.LogInfo("Boscali Ocean Training Exercise Loaded");
		
	}

	[Conditional("DEBUG")]
	internal static void DebugLog(string msg)
	{
		Logger.LogInfo(msg);
	}

	private static void InitializeMirageReaderWriters(Assembly assembly)
	{
		foreach (var type in assembly.GetTypes())
		{
			if (type.Name != "GeneratedNetworkCode") continue;
			RuntimeHelpers.RunClassConstructor(type.TypeHandle);

			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				if (method.Name.StartsWith("InitReadWriters"))
				{
					method.Invoke(null, null);
				}
			}
		}
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
}