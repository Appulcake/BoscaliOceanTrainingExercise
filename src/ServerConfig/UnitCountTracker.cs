using System.Collections.Generic;
using HarmonyLib;
using Mirage;
using NOComponentWIP;
using NOComponentWIP.ServerConfig;
using NuclearOption.Networking;
using UnityEngine;

[NetworkMessage]
public struct NetworkPlayerCount
{
    public string JsonKey;
    public int PlayerCount;
    public int FactionCount;
}

public class DeployedUnit
{
    public string JsonKey;
    public uint NetID;
    public ulong PlayerID;
    public string FactionID;
}

[HarmonyPatch]
public static class UnitCountTracker
{
    private static NetworkServer server;
    private static NetworkClient client;
	
    private static readonly Dictionary<uint, DeployedUnit> DeployedUnits = new();
	
    private static readonly Dictionary<(ulong SteamID, string JsonKey), int> PlayerCounts = new();
    private static readonly Dictionary<(string Faction, string JsonKey), int> FactionCounts = new();

    public static void RegisterListeners(NetworkServer Server, NetworkClient Client)
    {
        server ??= Server;
        client ??= Client;
        server?.Stopped.AddListener(Reset);
        Reset();
    }
    
    public static void RegisterHandlers(NetworkServer Server, NetworkClient Client)
    {
        server ??= Server;
        client ??= Client;
        Client?.MessageHandler.RegisterHandler<NetworkPlayerCount>(OnReceiveCountUpdate);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ServerApplyFaction))]
    [HarmonyPostfix]
    private static void ServerApplyFaction_Postfix(Player __instance)
    {
        SyncPlayerState(__instance.Owner);
    }

    public static void Reset()
    {
        DeployedUnits.Clear();
        PlayerCounts.Clear();
        FactionCounts.Clear();
    }

    public static void RegisterUnit(Unit unit, ulong ownerID)
    {
        if (unit == null || ownerID == 0 || unit.persistentID.NotValid) return;

        uint netId = unit.persistentID.Id;
        string jsonKey = unit.definition.jsonKey;
        string factionName = unit.NetworkHQ?.faction?.factionName ?? "Unassigned";
        
        if (DeployedUnits.ContainsKey(netId)) return;
        
        Plugin.DebugLog($"Tracking Unit: {jsonKey} : {netId}");

        var record = new DeployedUnit
        {
            JsonKey = jsonKey,
            NetID = netId,
            PlayerID = ownerID,
            FactionID = factionName
        };

        DeployedUnits[netId] = record;
        
        IncrementCount(ownerID, factionName, jsonKey);
        
        unit.onDisableUnit += UnregisterSpawn;
        
        SendUnitCount(jsonKey);
    }

    private static void UnregisterSpawn(Unit unit)
    {
        if (unit == null || unit.persistentID.NotValid) return;

        uint netId = unit.persistentID.Id;
        unit.onDisableUnit -= UnregisterSpawn;
        

        if (DeployedUnits.Remove(netId, out var record))
        {
            Plugin.DebugLog($"No longer tracking Unit: {record.JsonKey} : {netId}");
            DecrementCount(record.PlayerID, record.FactionID, record.JsonKey);
            
            SendUnitCount(record.JsonKey);
        }
    }
	
    public static void SendUnitCount(string jsonKey)
    {
        if (server == null || string.IsNullOrEmpty(jsonKey)) return;

        foreach (var networkPlayer in server.AuthenticatedPlayers)
        {
            if (!networkPlayer.TryGetPlayer(out Player player)) continue;

            string playerFaction = player.HQ?.faction?.factionName ?? string.Empty;

            var data = new NetworkPlayerCount
            {
                JsonKey = jsonKey,
                PlayerCount = GetPlayerCount(jsonKey, player.SteamID),
                FactionCount = GetFactionCount(jsonKey, playerFaction)
            };

            networkPlayer.Send(data);
        }
    }

    public static void Resync()
    {
        foreach (var player in server.AuthenticatedPlayers)
        {
            SyncPlayerState(player);
        }
    }
	
    public static void SyncPlayerState(INetworkPlayer networkPlayer)
    {
	    Player player = null;
	    networkPlayer?.TryGetPlayer(out player);
	    
        if (networkPlayer == null || player == null) return;

        string playerFaction = player.HQ?.faction?.factionName ?? string.Empty;
        
        HashSet<string> activeKeys = new();
        foreach (var unit in DeployedUnits.Values)
        {
            activeKeys.Add(unit.JsonKey);
        }

        foreach (var key in activeKeys)
        {
            var data = new NetworkPlayerCount
            {
                JsonKey = key,
                PlayerCount = GetPlayerCount(key, player.SteamID),
                FactionCount = GetFactionCount(key, playerFaction)
            };

            networkPlayer.Send(data);
        }
    }

    private static void OnReceiveCountUpdate(NetworkPlayerCount msg)
    {
        UnitConfig.UpdateCounts(msg.JsonKey, msg.PlayerCount, msg.FactionCount);
    }

    private static void IncrementCount(ulong steamId, string faction, string jsonKey)
    {
        var pKey = (steamId, jsonKey);
        PlayerCounts[pKey] = PlayerCounts.GetValueOrDefault(pKey, 0) + 1;

        var fKey = (faction, jsonKey);
        FactionCounts[fKey] = FactionCounts.GetValueOrDefault(fKey, 0) + 1;
    }

    private static void DecrementCount(ulong steamId, string faction, string jsonKey)
    {
        var pKey = (steamId, jsonKey);
        if (PlayerCounts.ContainsKey(pKey))
        {
            PlayerCounts[pKey] = Mathf.Max(0, PlayerCounts[pKey] - 1);
        }

        var fKey = (faction, jsonKey);
        if (FactionCounts.ContainsKey(fKey))
        {
            FactionCounts[fKey] = Mathf.Max(0, FactionCounts[fKey] - 1);
        }
    }

    public static int GetPlayerCount(string jsonKey, ulong steamID)
    {
        return PlayerCounts.GetValueOrDefault((steamID, jsonKey), 0);
    }

    public static int GetFactionCount(string jsonKey, string factionName)
    {
        if (string.IsNullOrEmpty(factionName)) return 0;
        return FactionCounts.GetValueOrDefault((factionName, jsonKey), 0);
    }
}