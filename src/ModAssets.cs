using System;
using System.Collections.Generic;
using Mirage;
using NuclearOption.Networking;
using UnityEngine;

namespace NOComponentWIP;

[CreateAssetMenu(fileName = "ModAssets", menuName = "Bote/ModAssets")]
public class ModAssets : ScriptableObject
{
	private static ModAssets _instance;
	public static ModAssets i
	{
		get
		{
			if (_instance == null)
			{
				var assets = Resources.FindObjectsOfTypeAll<ModAssets>();
				if (assets.Length > 0)
				{
					_instance = assets[0];
					_instance.Initialize();
				}
			}
			return _instance;
		}
		internal set => _instance = value;
	}

	public GameObject FOBEditorUI;
	public GameObject FOBEditorRow;
	public GameObject CargoEditorUI;
	public GameObject CargoEditorRow;

	[SerializeField] public AircraftDefinition[] shipDefinitions;
	[SerializeField] public AircraftDefinition[] shipDefinitionsWithDeployer;

	[SerializeField] public BuildingDefinition dockDef;

	[SerializeField] public GameObject networkModSingletons;
	[SerializeField] public GameObject modSingletons;

	[SerializeField] public RadialMenuAction[] actionsToAdd;
	[SerializeField] private List<DeployableUnit> allDeployableUnits;

	public readonly Dictionary<string, DeployableUnit> AllDeployableUnits = new();

	private void Initialize()
	{
		foreach (var unit in allDeployableUnits)
		{
			AllDeployableUnits.TryAdd(unit.JsonKey, unit);
		}
	}
	
	private void OnEnable()
	{
		hideFlags = HideFlags.DontUnloadUnusedAsset;
	}
}
