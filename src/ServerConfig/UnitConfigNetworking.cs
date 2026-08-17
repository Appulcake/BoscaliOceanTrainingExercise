using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Mirage;
using NuclearOption.Networking;
using NuclearOption.Networking.Authentication;
using UnityEngine;

namespace NOComponentWIP.ServerConfig;

[NetworkMessage]
public struct NetworkUnitConfigData
{
	public float GlobalCostMultiplier;
	public bool UnitLimitsEnabled;
	public bool UnitEconomyEnabled;
	
	[MaxLength(128)]
	public NetworkUnitConfigEntry[] Units;
}

[NetworkMessage]
public struct NetworkUnitConfigEntry
{
	public string JsonKey;

	public bool Enabled;
	public float Cost;

	public int PlayerMax;
	public int FactionMax;
}

[NetworkMessage]
public struct NetworkFactionCount
{
	public string JsonKey;
	public int FactionCount;
}

[HarmonyPatch]
public static class UnitConfigSync
{
	[HarmonyPatch(typeof(NetworkManagerNuclearOption), nameof(NetworkManagerNuclearOption.Awake))]
	private static void NMNO_Awake_Postfix(NetworkManagerNuclearOption __instance)
	{
		Register(__instance.Server, __instance.Client );
		UnitCountTracker.Register(__instance.Server, __instance.Client);
	}
	
	public static void Register(NetworkServer Server, NetworkClient Client)
	{
		
		if (Client == null || Server == null) return;
		
		Client.MessageHandler.RegisterHandler<NetworkUnitConfigData>(OnReceiveServerConfig);
		Client.Disconnected.AddListener(OnClientDisconnected);
			
		Server.Authenticated.AddListener(OnPlayerAuthenticated);
	}

	private static void OnPlayerAuthenticated(INetworkPlayer player)
	{
		NetworkUnitConfigData data = ToNetworkConfig(UnitConfig.ActiveConfigData);
		
		player.Send(data);
		
		Plugin.Logger.LogInfo($"Sent config to player: {player}");
	}

	private static void OnReceiveServerConfig(NetworkUnitConfigData config)
	{
		var newConfig = ToUnitConfig(config);
		
		UnitConfig.LoadRemoteConfig(newConfig);
	}

	private static void OnClientDisconnected(ClientStoppedReason reason)
	{
		UnitConfig.RestoreConfig();
	}

	public static UnitConfigData ToUnitConfig(NetworkUnitConfigData config)
	{
		Dictionary<string, UnitConfigEntry> Units = new();

		if (config.Units != null)
		{
			foreach (var entry in config.Units)
			{
				Units.TryAdd(entry.JsonKey, new UnitConfigEntry
				{
					Enabled = entry.Enabled,
					Cost = (entry.Cost == -1f ? null : entry.Cost) ,
					PlayerMax = entry.PlayerMax,
					FactionMax = entry.FactionMax
				});
			}
		}
		
		return new UnitConfigData
		{
			GlobalCostMultiplier = config.GlobalCostMultiplier,
			UnitLimitsEnabled = config.UnitLimitsEnabled,
			UnitEconomyEnabled = config.UnitEconomyEnabled,
			Units = Units
		};
	}

	public static NetworkUnitConfigData ToNetworkConfig(UnitConfigData config)
	{
		List<NetworkUnitConfigEntry> Units = new List<NetworkUnitConfigEntry>(config.Units.Count);
		
		foreach (var kvp in config.Units)
		{
			Units.Add(new NetworkUnitConfigEntry
			{
				JsonKey = kvp.Key,
				Enabled = kvp.Value.Enabled,
				Cost = kvp.Value.Cost ?? -1f,
				PlayerMax = kvp.Value.PlayerMax,
				FactionMax = kvp.Value.FactionMax
			});
		}

		return new NetworkUnitConfigData
		{
			GlobalCostMultiplier = config.GlobalCostMultiplier,
			UnitLimitsEnabled = config.UnitLimitsEnabled,
			UnitEconomyEnabled = config.UnitEconomyEnabled,
			Units = Units.ToArray()
		};
	}
}