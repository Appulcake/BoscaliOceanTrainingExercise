using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NOComponentWIP;

public class DamageControlUI : ShipApp
{
	[SerializeField] private Slider displacementSlider;
	[SerializeField] private Slider hpSlider;
	[SerializeField] private List<DCButton> buttons;
	
	
	private ShipPartBridge bridge;
	private Aircraft aircraft;

	private AircraftShipPart currentPart;

	public override void Initialize(Aircraft aircraft, ShipPartBridge bridge)
	{
		this.bridge = bridge;
		this.aircraft = aircraft;
		foreach (var button in buttons)
		{
			button.Initialize(this, bridge);
		}
	}

	public void SelectPart(AircraftShipPart part)
	{
		currentPart = part;
	}

	public override void Refresh()
	{
		if (currentPart != null)
		{
			displacementSlider.value = currentPart.displacement / currentPart.originalDisplacement;
			hpSlider.value = currentPart.hitPoints / currentPart.originalHitPoints;
		}
	}
}