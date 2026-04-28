using System;
using System.Collections.Generic;
using HarmonyLib;
using NuclearOption.MissionEditorScripts.Buttons;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace NOComponentWIP;

public class BOTERadialMenu : SceneSingleton<BOTERadialMenu>
{
	public List<RadialMenuAction> RadialMenuActions;

	private List<RadialMenuAction> allowedActions;
	private List<GameObject> actionsBOTE;
	private GameObject actionContainer;

	private bool isMenuOpen = false;
	public bool Open => isMenuOpen;

	private string openBindingName = "BOTE - Radial Menu";
	private Aircraft aircraft;

	private float degreesPerAction;
	private RadialMenuMain radialMenuMain;
	private Player player;

	public override void Awake()
	{
		base.Awake();
		allowedActions = new List<RadialMenuAction>();
		actionsBOTE = new List<GameObject>();
	}

	private void OnEnable()
	{
		 player = ReInput.players.GetPlayer(0);
	}

	private void Update()
	{
		radialMenuMain = RadialMenuMain.i;
		if (radialMenuMain == null) return;
		
		aircraft = radialMenuMain.aircraft;
		if (actionContainer == null)
		{
			actionContainer = Instantiate(radialMenuMain.actionsMainContainer, radialMenuMain.actionsMainContainer.transform.parent);
		}

		bool inputHeld = player.GetButtonTimedPressDown(openBindingName, PlayerSettings.pressDelay);

		if (inputHeld && !isMenuOpen)
		{
			OpenMenu();
		} else if (!player.GetButton(openBindingName) && isMenuOpen)
		{
			CloseMenu();
		}
		UpdateMenu();
	}

	public void UpdateMenu()
	{
		if (isMenuOpen)
		{
			if (Time.realtimeSinceStartup - radialMenuMain.lastOpen > 0.05f && radialMenuMain.canvasGroup.alpha < 1f)
			{
				radialMenuMain.canvasGroup.alpha += 10f * Time.deltaTime;
			}
			else
			{
				radialMenuMain.canvasGroup.alpha = 1f;
			}
			if (!actionContainer.activeSelf)
			{
				actionContainer.SetActive(true);
			}
			radialMenuMain.CheckAction(allowedActions, degreesPerAction, openBindingName);
		}
		else
		{
			actionContainer.SetActive(false);
		}
	}

	private void OpenMenu()
	{
		isMenuOpen = true;
		
		radialMenuMain.OpenMenu();

		GameObject main = radialMenuMain.actionsMainContainer;
		GameObject weapons = radialMenuMain.actionsWeaponsContainer;
		
		main.SetActive(false);
		weapons.SetActive(false);
		radialMenuMain.menuObject.SetActive(true);

		SetupUI();
	}

	private void CloseMenu()
	{
		isMenuOpen = false;
		radialMenuMain.CloseMenu();
		
	}

	private void SetupUI()
	{
		allowedActions.Clear();
		foreach (var item in actionsBOTE)
		{
			Destroy(item);
		}
		actionsBOTE.Clear();
		foreach (var item in RadialMenuActions)
		{
			if (item.AllowedOnAircraft(aircraft))
			{
				allowedActions.Add(item);
			}
		}

		degreesPerAction = 360f / (float)allowedActions.Count;

		for (int j = 0; j < allowedActions.Count; j++)
		{
			GameObject gameObject = Instantiate(radialMenuMain.sectorObject, actionContainer.transform);
			Image component = gameObject.GetComponent<Image>();
			component.fillAmount = degreesPerAction / 360f;
			gameObject.transform.localEulerAngles = new Vector3(0f, 0f, (0f - ((float)j - 0.5f)) * degreesPerAction);
			_ = allowedActions[j];
			GameObject gameObject2 = Instantiate(radialMenuMain.actionPrefab, actionContainer.transform);
			Image component2 = gameObject2.GetComponent<Image>();
			Text component3 = gameObject2.transform.Find("Text").GetComponent<Text>();
			allowedActions[j].Setup(component, component2, component3);
			gameObject2.transform.localPosition = 90f * new Vector3(Mathf.Sin((float)j * degreesPerAction * (MathF.PI / 180f)), Mathf.Cos((float)j * degreesPerAction * (MathF.PI / 180f)), 0f);
			actionsBOTE.Add(gameObject2);
			actionsBOTE.Add(gameObject);
		}
	}
}

[HarmonyPatch(typeof(RadialMenuAction))]
public class CustomMenuAction : RadialMenuAction
{
	public enum CustomActionType
	{
		Deploy,
		FOB,
		NextUnit,
		PrevUnit,
		ResupplyAI
	}

	public void TriggerActionCustom(Aircraft aircraft)
	{
		Plugin.Logger.LogInfo(customActionType);
	}
	
	[SerializeField] private CustomActionType customActionType;

	[HarmonyPatch(nameof(RadialMenuAction.TriggerAction))]
	[HarmonyPrefix]
	private static bool TriggerAction_Prefix(RadialMenuAction __instance, Aircraft aircraft)
	{
		if (__instance is not CustomMenuAction action) return true;

		action.TriggerActionCustom(aircraft);
		
		return false;
	}
}