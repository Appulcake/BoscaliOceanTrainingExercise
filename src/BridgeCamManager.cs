using HarmonyLib;
using UnityEngine;

namespace NOComponentWIP;

[HarmonyPatch]
public class BridgeCamManager : MonoBehaviour
{
	[SerializeField] private Aircraft aircraft;
	
	[SerializeField] private Transform[] stations;
	private int currentIndex = 0;

	private void Start()
	{
		if (stations == null) return;
		if (stations.Length == 0) return;
		this.transform.position = stations[0].position;
		this.transform.rotation = stations[0].rotation;
	}
	
	private void Update()
	{
		if (CameraStateManager.i.currentState != CameraStateManager.i.cockpitState) return;
		if (stations.Length == 0) return;
		
		if (!GameManager.IsLocalAircraft(aircraft)) return;
		if (GameManager.playerInput.GetButtonDown("Next Camera"))
		{
			CycleCam(1);
		} else if (GameManager.playerInput.GetButtonDown("Previous Camera"))
		{
			CycleCam(-1);
		}
	}

	public void CycleCam(int direction)
	{
		CameraStateManager.i?.cockpitState?.panView = 0f;
		CameraStateManager.i?.cockpitState?.tiltView = 0f;
		currentIndex = (currentIndex + direction + stations.Length) % stations.Length;
		
		//FlightHud.i?.cockpitTransform = stations[currentIndex];
		
		this.transform.position = stations[currentIndex].position;
		this.transform.rotation = stations[currentIndex].rotation;
	}
	
	[HarmonyPostfix]
	[HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.EnterState))]
	private static void EnterState_Postfix(CameraCockpitState __instance, CameraStateManager cam, Quaternion __state)
	{
		cam.mainCamera.nearClipPlane = 0.2f;
		if (__instance.aircraft != null)
		{
			var camManager = __instance.aircraft.cockpitViewPoint.GetComponent<BridgeCamManager>();
			if (camManager != null)
			{
				__instance.aircraft.cockpitViewPoint.rotation = camManager.stations[camManager.currentIndex].rotation;
			}
		}
		
	}
}