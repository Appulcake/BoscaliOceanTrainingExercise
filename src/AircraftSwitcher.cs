using Cysharp.Threading.Tasks;
using HarmonyLib;
using Mirage;
using NuclearOption.Networking;
using NuclearOption.SceneLoading;
using UnityEngine;

namespace NOComponentWIP;

public class AircraftSwitcher : NetworkSceneSingleton<AircraftSwitcher>
{
	public void SwitchAircraft(Player player, Aircraft oldAircraft, Aircraft newAircraft)
	{
		RpcSwitchAircraft(player, oldAircraft, newAircraft);
	}
	
	[ServerRpc(requireAuthority = false)]
	private void RpcSwitchAircraft(Player player, Aircraft oldAircraft, Aircraft newAircraft)
	{
		if (oldAircraft == null || newAircraft == null) return;
		if (player.Identity.Owner != oldAircraft.Identity.Owner) return;
		if (newAircraft.Player != null) return;
		if (newAircraft.NetworkHQ != player.HQ) return;

		oldAircraft.playerRef = new PlayerRef();
		player.RemoveAircraft(oldAircraft);
		player.RemoveAircraftAuthority(oldAircraft);
		if (oldAircraft.autopilot != null)
		{
			oldAircraft.pilots[0].SetStartingAiState();
		}
		else
		{
			oldAircraft.pilots[0].SwitchState(oldAircraft.pilots[0].parkedState);
		}
		oldAircraft.pilots[0].player = null;
		oldAircraft.SetLocalSim(oldAircraft.CheckIfLocalSim());
		oldAircraft.weaponManager.ClearTargetList();

		if (UnitRegistry.TryGetPersistentUnit(oldAircraft.persistentID, out var persistentUnit))
		{
			persistentUnit.player = null;
		}

		newAircraft.playerRef = new PlayerRef(player);
		newAircraft.Identity.AssignClientAuthority(player.Owner);
		newAircraft.SetLocalSim(newAircraft.CheckIfLocalSim());
		player.SetAircraft(newAircraft);
		newAircraft.pilots[0].SwitchState(null);
		/*newAircraft.NetworkHQ = player.HQ; //funny mode*/
		oldAircraft.pilots[0].aircraft = oldAircraft;
		
		CmdSwitchAircraft(player, oldAircraft, newAircraft);
	}

	[ClientRpc]
	private void CmdSwitchAircraft(Player player, Aircraft oldAircraft, Aircraft newAircraft)
	{
		oldAircraft?.SetLocalSim(oldAircraft.CheckIfLocalSim());
		newAircraft?.SetLocalSim(newAircraft.CheckIfLocalSim());
		
		if (GameManager.IsLocalPlayer(player))
		{
			if (oldAircraft != null)
			{
				if (oldAircraft.statusDisplay != null)
				{
					oldAircraft.onDisableUnit -= oldAircraft.statusDisplay.StatusDisplay_OnDisable;
					Destroy(oldAircraft.statusDisplay.gameObject);
				}
				oldAircraft.weaponManager.currentWeaponStation.SetStationActive(oldAircraft, false);
				
				foreach (Transform cam in oldAircraft.targetCam?.currentMount.transform)
				{
					Destroy(cam.gameObject);
				}
				Destroy(oldAircraft.targetCam?.targetScreenUI);
				Destroy(oldAircraft.targetCam?.landingScreenUI);
				oldAircraft.onDisableUnit -= CombatHUD.i.threatList.ThreatList_OnAircraftDisable;
				CombatHUD.i.threatList.ThreatList_OnAircraftDisable(oldAircraft);

				if (HUDAppManager.i != null && MFDAppManager.i != null)
				{
					oldAircraft.onDisableUnit -= HUDAppManager.i.HUDAppManager_OnUnitDisable;
					oldAircraft.onDisableUnit -= MFDAppManager.i.HUDAppManager_OnUnitDisable;
					PlayerSettings.OnApplyOptions -= HUDAppManager.i.RefreshSettings;
					PlayerSettings.OnApplyOptions -= MFDAppManager.i.RefreshSettings;
					Destroy(HUDAppManager.i.gameObject);
					Destroy(MFDAppManager.i.gameObject);
				}
				
				var oldCockpit = newAircraft.cockpit?.GetComponentInChildren<Cockpit>();
				if (oldCockpit != null)
				{
					oldCockpit.enabled = false;
					oldAircraft.onDisableUnit -=
						oldCockpit.Cockpit_OnAircraftDisable;
					Destroy(oldCockpit.tacScreen);
				}
			}

			CombatHUD.i.RemoveAircraft();
			CombatHUD.i.SetAircraft(newAircraft);
			DynamicMap.i.DeselectAllIcons();
			
			if (newAircraft != null)
			{
				newAircraft.weaponManager.currentWeaponStation.SetStationActive(newAircraft, true);
				CombatHUD.i.weaponStatus.UpdateDisplay(newAircraft.weaponManager.currentWeaponStation);
				
				foreach (var missile in newAircraft.GetMissileWarningSystem().knownMissiles)
				{
					CombatHUD.i.threatList.ThreatList_OnMissileWarning(new MissileWarning.OnMissileWarning
					{
						missile = missile
					});
				}
				
				newAircraft.pilots[0].SwitchState(newAircraft.pilots[0].playerState);
				newAircraft.SetupLocalPlayerAndUI();
				
				var newCockpit = newAircraft.cockpit?.GetComponentInChildren<Cockpit>();
				
				if (newCockpit != null)
				{
					newCockpit.Cockpit_OnAircraftInitialize();
				}
			
				newAircraft.targetCam?.Initialize();
				newAircraft.weaponManager.ClearTargetList();
			}
		}
	}
}

[HarmonyPatch(typeof(MapLoader))]
public class AircraftSwitcherSpawnPatch
{
	[HarmonyPatch(nameof(MapLoader.LoadScene))]
	[HarmonyPostfix]
	private static async UniTask<MapLoader.LoadResult> Postfix(UniTask<MapLoader.LoadResult> __result, MapLoader.SceneKey key)
	{
		MapLoader.LoadResult status = await __result;

		if (status == MapLoader.LoadResult.ChangedScene && key.Path.Contains("GameWorld"))
		{
			SetupScene();
		}
		return status;
	}

	private static void SetupScene()
	{
		var target = GameObject.Find("SceneEssentials");

		if (ModAssets.i.networkModSingletons != null)
		{
			var networkSingletons = Object.Instantiate(ModAssets.i.networkModSingletons, target.transform, true);
			NetworkManagerNuclearOption.i.ServerObjectManager.Spawn(networkSingletons.GetNetworkIdentity());
		}

		if (ModAssets.i.modSingletons != null)
		{
			var singletons = Object.Instantiate(ModAssets.i.modSingletons, target.transform, true);
		}
		
		
		
	}
}