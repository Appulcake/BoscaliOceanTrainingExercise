using System;
using System.Collections.Generic;
using Mirage;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BoscaliOceanTrainingExercise;

public static class TransformExtensions
{
	public static T GetComponentInParentWithDepth<T>(this Transform startTransform, int maxDepth) where T : Component
	{
		Transform current = startTransform;
		for (int i = 0; i <= maxDepth && current != null; i++)
		{
			if (current.TryGetComponent<T>(out var component)) return component;
			current = current.parent;
		}
		return null;
	}
}

public static class UnitDefinitionExtensions
{
	public static bool IsShipDefinition(this UnitDefinition definition)
	{
		return ModAssets.i.ShipDefinitions.Contains(definition as AircraftDefinition);
	}
	
	public static bool IsShipDefinitionWithDeployer(this UnitDefinition definition)
	{
		return ModAssets.i.ShipDefinitionsWithDeployer.Contains(definition as AircraftDefinition);
	}
}

public static class AircraftExtensions
{
	private static ConditionalWeakTable<Aircraft, ShipPartBridge> cache = new();

	public static bool TryGetShipBridge(this Aircraft aircraft, out ShipPartBridge bridge)
	{
		bridge = null;
		if (!aircraft.definition.IsShipDefinition()) return false;
		if (cache.TryGetValue(aircraft, out bridge)) return true;
		bridge = aircraft?.GetComponent<ShipPartBridge>();
		if (bridge != null)
		{
			cache.Add(aircraft, bridge);
			return true;
		}

		return false;
	}
}

public static class HQExtensions
{
	public static bool GetNearestAircraftCapableAirbase(this FactionHQ hq, Vector3 position, AircraftDefinition[] definitions, out Airbase validAirbase, Airbase exclude = null)
	{
		validAirbase = null;
		if (hq == null) return false;
		
		var sortedBases = hq.airbasesUnsorted
			.Select(item => item.Value)
			.Where(ab => ab != null && !ab.disabled && ab.CurrentHQ == hq)
			.OrderBy(ab => Vector3.Distance(position, ab.transform.position));

		foreach (Airbase airbase in sortedBases)
		{
			if (airbase == exclude) continue;
			foreach (var hangar in airbase.hangars)
			{
				if (hangar != null && !hangar.Disabled && hangar.availableAircraft.Any(definitions.Contains))
				{
					validAirbase = airbase;
					return true; 
				}
			}
		}

		return false;
	}

	public static bool AnyNearAirbaseInRange(this FactionHQ hq, Vector3 fromPosition, out Airbase airbase, float range = 1000, Airbase excludeAirbase = null)
	{
		airbase = null;
		foreach (NetworkBehaviorSyncvar<Airbase> item in hq.airbasesUnsorted)
		{
			Airbase ab = item.Value;
			if (ab != null)
			{
				if (excludeAirbase == ab) continue;
				if (FastMath.InRange(fromPosition, ab.center.position, range))
				{
					airbase = ab;
					return true;
				}
			}
		}

		return false;
	}

	public static bool TryGetNearestAircraft(this FactionHQ hq, GlobalPosition fromPosition, out Aircraft nearestAircraft, out float nearestDistance, Aircraft excludeAircraft = null)
	{
		nearestAircraft = null;
		nearestDistance = float.MaxValue;
		foreach (PersistentID factionUnit in hq.factionUnits)
		{
			if (UnitRegistry.TryGetUnit(factionUnit, out var unit) && unit is Aircraft aircraft)
			{
				if (aircraft == excludeAircraft) continue;
				float num = FastMath.SquareDistance(aircraft.GlobalPosition(), fromPosition);
				if (num < nearestDistance)
				{
					nearestAircraft = aircraft;
					nearestDistance = num;
				}
			}
		}
		return nearestAircraft != null;
	}
}


//please let me use .netstandard2.1 for transpilers :(

public static class DictionaryExtensions
{
	public static TValue GetValueOrDefault<TKey, TValue>(
		this IDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.TryGetValue(key, out TValue value) ? value : default(TValue);
	}
	
	public static TValue GetValueOrDefault<TKey, TValue>(
		this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
	{
		return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
	}
	
	public static bool Remove<TKey, TValue>(
		this IDictionary<TKey, TValue> dictionary, 
		TKey key, 
		out TValue value)
	{
		if (dictionary == null)
		{
			throw new System.ArgumentNullException(nameof(dictionary));
		}
		
		if (dictionary.TryGetValue(key, out value))
		{
			return dictionary.Remove(key);
		}
		
		value = default;
		return false;
	}
	
	public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException(nameof(dictionary));
		}

		if (dictionary.ContainsKey(key))
		{
			return false;
		}

		dictionary.Add(key, value);
		return true;
	}
}

public static class KeyValuePairExtensions
{
	public static void Deconstruct<TKey, TValue>(
		this KeyValuePair<TKey, TValue> kvp, 
		out TKey key, 
		out TValue value)
	{
		key = kvp.Key;
		value = kvp.Value;
	}
}
