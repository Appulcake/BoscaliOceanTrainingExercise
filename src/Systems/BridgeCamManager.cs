using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP.Systems;

[HarmonyPatch]
public class BridgeCamManager : MonoBehaviour
{
	[SerializeField] private Aircraft aircraft;
	[SerializeField] private Transform[] stations;
	private int currentIndex = 0;

	private const string CAM_NEXT = $"{Mod_Input.ModShortName}:Next Camera";
	private const string CAM_PREV = $"{Mod_Input.ModShortName}:Previous Camera";

	private void Start()
	{
		if (stations == null) return;
		if (stations.Length == 0) return;
		if (stations[0] == null) return;
		this.transform.SetPositionAndRotation(stations[0].position, stations[0].rotation);
	}
	
	private void Update()
	{
		if (CameraStateManager.i.currentState != CameraStateManager.i.cockpitState) return;
		if (stations.Length == 0) return;
		
		if (!GameManager.IsLocalAircraft(aircraft)) return;
		if (GameManager.playerInput.GetButtonDown(CAM_NEXT))
		{
			CycleCam(1);
		} else if (GameManager.playerInput.GetButtonDown(CAM_PREV))
		{
			CycleCam(-1);
		}
	}

	public void CycleCam(int direction)
	{
		if (stations  == null || stations.Length == 0) return;
		
		CameraStateManager.i?.cockpitState?.panView = 0f;
		CameraStateManager.i?.cockpitState?.tiltView = 0f;
		currentIndex = (currentIndex + direction + stations.Length) % stations.Length;
		
		//FlightHud.i?.cockpitTransform = stations[currentIndex];
		
		var station = stations[currentIndex];
		if (stations == null) return;
		
		this.transform.SetPositionAndRotation(station.position, station.rotation);
	}
	
	[HarmonyPostfix]
	[HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.EnterState))]
	private static void EnterState_Postfix(CameraCockpitState __instance, CameraStateManager cam, Quaternion __state)
	{
		cam?.mainCamera?.nearClipPlane = 0.2f;
		if (__instance.aircraft != null)
		{
			var cockpitViewPoint = __instance.aircraft?.cockpitViewPoint;
			if (cockpitViewPoint == null) return;
			var camManager = cockpitViewPoint.GetComponent<BridgeCamManager>();
			if (camManager != null && camManager.stations != null && camManager.stations.Length > camManager.currentIndex
			    && camManager.stations[camManager.currentIndex] != null)
			{
				cockpitViewPoint.rotation = camManager.stations[camManager.currentIndex].rotation;
			}
		}
	}
}