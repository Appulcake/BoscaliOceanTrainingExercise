using System.Collections;
using System.Collections.Generic;
using Mirage;
using NuclearOption.Networking;
using UnityEngine;

namespace NOComponentWIP;

public class FOBManager : NetworkBehaviour
{
    [SerializeField] private Aircraft aircraft;
    public List<FOBUnit> availableFOBUnits;
    
    [SyncVar] public bool hasFob;
    
    public bool BuildingFob { get; private set; }
    
    private GameObject fobUI;
    private Coroutine fobCoroutine;
    
	[ClientRpc(target = RpcTarget.Owner)]
	public void DeployFOB()
	{
        if (aircraft == null || !aircraft.LocalSim) return;

        if (fobCoroutine != null)
        {
            StopCoroutine(fobCoroutine);
        }
        
		fobCoroutine = StartCoroutine(FOBBuilder());
	}
	
	private IEnumerator FOBBuilder()
    {
        var canvas = GameplayUI.i.gameplayCanvas;
        if (canvas == null) yield break;
        
        BuildingFob = true;
        
        CursorManager.SetFlag(CursorFlags.Map, value: true);
        DynamicMap.AllowedToOpen = false;
        GameManager.flightControlsEnabled = false;
        LoadoutBridge.BlockInputs = true;
        
        aircraft.onDisableUnit += Disable;
        
        fobUI = Instantiate(ModAssets.i.FOBEditorUI, canvas.transform);
        var uiController = fobUI.GetComponent<FOBUIController>();
        
        uiController.Initialize(this, aircraft, aircraft.rb.position, availableFOBUnits,160);
        
        yield return new WaitUntil(() => !BuildingFob || aircraft.Networkdisabled); //will be changed to check when fob is done
        
        Cleanup();
    }

    private void Cleanup()
    {
        BuildingFob = false;

        if (fobUI != null)
        {
            Destroy(fobUI);
            fobUI = null;
        }
        
        this.aircraft?.onDisableUnit -= Disable;
        if (!aircraft?.LocalSim ?? true) return;
        
        CursorManager.SetFlag(CursorFlags.Map, value: false);
        DynamicMap.AllowedToOpen = true;
        LoadoutBridge.BlockInputs = false;
        GameManager.flightControlsEnabled = true;
    }
    
    private void Disable(Unit unit)
    {
        Cleanup();
    }

    public void Close()
    {
        BuildingFob = false;
    }

    public void FinalizeFOB(List<PlacedFOBUnit> placedUnits, bool spawnAirbase, Vector3 center)
    {
        int count = placedUnits.Count;
        
        int[] indices = new int[count];
        Vector3[] positions = new Vector3[count];
        Quaternion[] rotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            var unit = placedUnits[i];
            indices[i] = availableFOBUnits.IndexOf(unit.data);
            positions[i] = unit.globalPosition;
            rotations[i] = unit.rotation;
        }
        
        CmdFinalizeFOB(indices, positions, rotations, spawnAirbase, center);
    }

    [ServerRpc]
    private void CmdFinalizeFOB(int[] indices, Vector3[] positions, Quaternion[] rotations, bool spawnAirbase, Vector3 center)

    {
        if (indices == null || positions == null || rotations == null ||
            indices.Length != positions.Length || indices.Length != rotations.Length)
        {
            Plugin.Logger.LogError("Network array mismatch on CmdFinalizeFOB! Aborting spawn.");
            return;
        }

        Airbase airbase = null;
        
        if (spawnAirbase)
        {
            SetupAirbase(center, out airbase);
        }
        
        for (int i = 0; i < indices.Length; i++)
        {
            int dataIndex = indices[i];
            if (dataIndex < 0 || dataIndex >= availableFOBUnits.Count) continue;

            var data = availableFOBUnits[dataIndex];
            if (data == null) continue;
            
            var gp = new GlobalPosition(positions[i]);
            var spawnedObj = data.SpawnUnit(gp.ToLocalPosition(), rotations[i], Vector3.zero, aircraft, true, out var spawned);
            
            if (spawned && spawnedObj != null && spawnAirbase && airbase != null)
            {
                var building = spawnedObj.GetComponent<Building>();
                if (building != null)
                {
                    building.SetAirbase(airbase);
                }
            }
        }

        hasFob = false;
    }

    private void SetupAirbase(Vector3 center, out Airbase airbase)
    {
        GameObject go = Instantiate(GameAssets.i.airbasePrefab, Datum.origin);
        string uname = $"FOB_{aircraft.Player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard)}_{Time.time}";
        var displayName = $"FOB: {aircraft.Player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard)}";
            
        go.name = uname;
            
        var filter = go.AddComponent<AirbaseAIFilter>();
        filter.AddAllowedKey("UtilityHelo1");
        filter.AddAllowedKey("AttackHelo1");
        filter.AddAllowedKey("QuadVTOL1");
            
        airbase = go.GetComponent<Airbase>();
        if (airbase != null)
        {
            var globalCenter = new GlobalPosition(center.x, center.y + 10f, center.z);
            airbase.transform.position = globalCenter.ToLocalPosition();
            airbase.aircraftSelectionTransform = airbase.transform;
            airbase.center.localPosition = Vector3.zero;
            airbase.airbaseSettings.CaptureRange = 100f;
                
            airbase.SavedAirbase.UniqueName = uname;
            airbase.SavedAirbase.DisplayName = displayName;
                
            airbase.capture.SetCapturable(true);
            airbase.CaptureFaction(aircraft.NetworkHQ);
                
            NetworkManagerNuclearOption.i.ServerObjectManager.Spawn(airbase.Identity);
            RpcFinalizeFOB(airbase, globalCenter.AsVector3(), displayName);
        }
    }

    [ClientRpc]
    private void RpcFinalizeFOB(Airbase airbase, Vector3 globalCenter, string displayName)
    {
        if (airbase == null) return;
        
        airbase.transform.position = new GlobalPosition(globalCenter).ToLocalPosition();
        airbase.aircraftSelectionTransform = airbase.transform;
        airbase.center.localPosition = Vector3.zero;
        
        airbase.SavedAirbase.DisplayName = displayName;
    }
    
    [ServerRpc]
    public void ResetFOB()
    {
        hasFob = true;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}