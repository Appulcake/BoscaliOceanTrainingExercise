using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NOComponentWIP;

public class UnitRowController : MonoBehaviour
{
	[Header("Display")]
	[SerializeField] private TextMeshProUGUI unitNameText;
	[SerializeField] private TextMeshProUGUI unitCostText;
	[SerializeField] private TextMeshProUGUI countText;
	[SerializeField] private Image unitIcon;

	[Header("Controls")]
	[SerializeField] private Button plusButton;
	[SerializeField] private Button minusButton;

	private DeployableUnit unit;
	private int currentCount;
	private int unitCost;
	private CargoUIController uiController;

	public void Setup(DeployableUnit unit, int initialCount, CargoUIController uiController)
	{
		if (unit == null || uiController == null) return;
		
		this.unit = unit; 
		this.uiController = uiController;
		currentCount = initialCount;
		unitCost = unit.pointCost;
        
		unitNameText.text = unit.unitName;
		unitCostText.text = $"[{unitCost}]";
		if (unitIcon != null) unitIcon.sprite = unit.icon;

		if (plusButton != null)
		{
			plusButton.onClick.RemoveAllListeners();
			plusButton.onClick.AddListener(() => OnButtonClick(1));
		}

		if (minusButton != null)
		{
			minusButton.onClick.RemoveAllListeners();
			minusButton.onClick.AddListener(() => OnButtonClick(-1));
		}
		
		UpdateLocalDisplay();
	}

	private void OnButtonClick(int delta)
	{
		uiController.ChangeUnitCount(unit, delta, unitCost, out int deltaActual);
        
		currentCount = Mathf.Max(0, currentCount + deltaActual); 
        
		UpdateLocalDisplay();
	}

	public void UpdateLocalDisplay()
	{
		countText.text = currentCount.ToString("D2"); 
        
		minusButton.interactable = currentCount > 0;
	}
	
	public void UpdateAbilityToIncrement(int totalPoints, int maxPoints)
	{
		bool canAfford = (totalPoints + unitCost) <= maxPoints;
		plusButton.interactable = canAfford;
        
		plusButton.GetComponentInChildren<TextMeshProUGUI>().color = canAfford ? Color.white : new Color(1,1,1,0.2f);
	}

	private void OnDestroy()
	{
		plusButton?.onClick.RemoveAllListeners();
		minusButton?.onClick.RemoveAllListeners();
	}
}