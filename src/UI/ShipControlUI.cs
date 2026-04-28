using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOComponentWIP;

public class ShipControlUI : MonoBehaviour
{
	[System.Serializable]
	public struct UITab
	{
		public string name;
		public Button button;
		public GameObject panelObject;
	}
	
	public List<UITab> uiTabs = new List<UITab>();
	public Color activeColor = Color.green;
	public Color inactiveColor = Color.gray;
	public List<ShipApp> apps;

	[SerializeField] private Canvas canvas;
	[SerializeField] private GraphicRaycaster raycaster;

	private int tabIndex = -1;
	private Aircraft aircraft;

	private void Awake()
	{
		raycaster.enabled = false;
		enabled = false;
	}
	
	private void Start()
	{
		for (int i = 0; i < uiTabs.Count; i++)
		{
			int index = i;
			uiTabs[i].button.onClick.AddListener(() => SwitchToTab(index));
		}
		canvas.worldCamera = CameraStateManager.i.mainCamera;
	}

	public void Initialize(Aircraft aircraft, ShipPartBridge bridge)
	{
		this.aircraft = aircraft;
		foreach (var app in apps)
		{
			app.Initialize(aircraft, bridge);
		}
		raycaster.enabled = true;
		CombatHUD.i?.targetDesignator?.raycastTarget = false;
		aircraft.onDisableUnit += ShipControlUI_OnDisable;
		enabled = true;
	}

	private void ShipControlUI_OnDisable(Unit unit)
	{
		raycaster.enabled = false;
		CombatHUD.i?.targetDesignator?.raycastTarget = true;
		Destroy(this.gameObject);
	}

	private void SwitchToTab(int index)
	{
		if (index == tabIndex || index < 0 || index >= uiTabs.Count) return;
		
		for (int i = 0; i < uiTabs.Count; i++)
		{
			bool isActive = (i == index);
			uiTabs[i].panelObject?.SetActive(isActive);
			
			var colors = uiTabs[i].button.colors;
			uiTabs[i].button.image.color = isActive ? activeColor : inactiveColor;
		}

		tabIndex = index;
	}

	private void Update()
	{
		foreach (var app in apps)
		{
			app.Refresh();
		}
		var pd = new PointerEventData(GameManager.eventSystem)
		{
			position = new Vector2(Screen.width / 2, Screen.height / 2)
		};

		var results = new List<RaycastResult>();
		raycaster.Raycast(pd, results);

		var hit = results.Count < 0 ? results[0].gameObject : null;
		var player = aircraft.pilots[0]?.playerState?.player;
		
		if (hit != null &&  player != null)
		{
			if (player.GetButtonDown("Control UI - Select")) {
				ExecuteEvents.Execute(hit, pd, ExecuteEvents.pointerDownHandler);
			}
            
			if (player.GetButtonUp("Control UI - Select")) {
				ExecuteEvents.Execute(hit, pd, ExecuteEvents.pointerUpHandler);
				ExecuteEvents.Execute(hit, pd, ExecuteEvents.pointerClickHandler);
			}
		}
	}
}