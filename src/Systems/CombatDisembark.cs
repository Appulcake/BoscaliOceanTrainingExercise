using System.Collections.Generic;
using Mirage;
using NOComponentWIP.ServerConfig;
using UnityEngine;

namespace NOComponentWIP;

[NetworkMessage]
public struct CombatDisembarkStateMessage
{
    public float RemainingSeconds;
}

public static class CombatDisembark
{
    private static readonly Dictionary<string, float> WeaponDurationCache = new();
    
    public static void ClearWeaponCache()
    {
        WeaponDurationCache.Clear();
    }
    
    public static void RegisterLaunch(Aircraft aircraft, MissileDefinition definition)
    {
        if (aircraft == null || definition == null || !aircraft.IsServer || aircraft.Player == null ||
            !aircraft.TryGetShipBridge(out var bridge)) return;
        
        var duration = GetCombatDuration(definition);
        if (duration <= 0f || !bridge.ExtendServerCombatLock(duration, out var remaining)) return;
        
        aircraft.Player.Owner?.Send(new CombatDisembarkStateMessage
        {
            RemainingSeconds = remaining
        });
    }
    
    private static float GetCombatDuration(MissileDefinition definition)
    {
        var config = CombatDisembarkConfig.Active;
        if (!config.CombatTimerEnabled) return 0f;
        
        var key = definition.jsonKey ?? string.Empty;
        if (WeaponDurationCache.TryGetValue(key, out var cached)) return cached;
        
        var duration = 0f;
        var seeker = definition.unitPrefab?.GetComponent<MissileSeeker>();
        
        duration = seeker switch
        {
            OpticalSeekerCruiseMissile => config.CruiseMissileTimer,
            BallisticMissileGuidance => config.BallisticMissileTimer,
            OpticalSeeker => config.OpticalCombatTimer,
            _ => duration
        };
        
        duration = Mathf.Max(0f, duration);
        WeaponDurationCache[key] = duration;
        return duration;
    }
    
    public static void RegisterHandlers(NetworkClient client)
    {
        client?.MessageHandler.RegisterHandler<CombatDisembarkStateMessage>(OnCombatStateReceived);
    }
    
    private static void OnCombatStateReceived(CombatDisembarkStateMessage message)
    {
        if (!GameManager.GetLocalAircraft(out var aircraft) || !aircraft.TryGetShipBridge(out var bridge)) return;
        bridge.SetClientCombatRemaining(message.RemainingSeconds);
    }
}