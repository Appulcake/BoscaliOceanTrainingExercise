using System.Collections.Generic;
using UnityEngine;
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

	private int tabIndex = -1;

	private void Start()
	{
		for (int i = 0; i < uiTabs.Count; i++)
		{
			int index = i;
			uiTabs[i].button.onClick.AddListener(() => SwitchToTab(index));
		}
	}

	public void Initialize(Aircraft aircraft, ShipPartBridge bridge)
	{
		foreach (var app in apps)
		{
			app.Initialize(aircraft, bridge);
		}
	}

	private void SwitchToTab(int index)
	{
		if (index == tabIndex || index < 0 || index >= uiTabs.Count) return;
		
		for (int i = 0; i < uiTabs.Count; i++)
		{
			bool isActive = (i == index);
			uiTabs[i].panelObject.SetActive(isActive);
			
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
	}
}