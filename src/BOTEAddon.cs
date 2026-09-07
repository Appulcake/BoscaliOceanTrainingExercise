using System.Collections.Generic;
using UnityEngine;

namespace BoscaliOceanTrainingExercise;

public class BOTEAddon : ScriptableObject
{
	[Header("Mod Info (for logging)")]
	[SerializeField] private string modName;
	
	[Header("Lists")]
	[SerializeField] private AircraftDefinition[] shipDefinitions;
	[SerializeField] private AircraftDefinition[] shipDefinitionsWithDeployer;
	[SerializeField] private DeployableUnit[] allDeployableUnits;
	[SerializeField] private RadialMenuAction[] actionsToAdd;
	
	public virtual void Initialize(ModAssets i)
	{
		Plugin.Logger.LogInfo($"Initializing addon: {modName}");
		
		foreach (var unit in allDeployableUnits)
		{
			i.AllDeployableUnits.TryAdd(unit?.JsonKey, unit);
		}

		foreach (var def in shipDefinitions)
		{
			i.ShipDefinitions.Add(def);
		}
		Plugin.Logger.LogInfo($"[{modName}]: Loaded {shipDefinitions.Length} ships");

		foreach (var def in shipDefinitionsWithDeployer)
		{
			i.ShipDefinitionsWithDeployer.Add(def);
		}

		foreach (var action in actionsToAdd)
		{
			i.ActionsToAdd.Add(action);
		}
	}
}