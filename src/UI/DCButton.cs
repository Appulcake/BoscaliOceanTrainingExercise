using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NOComponentWIP;

public class DCButton : MonoBehaviour
{
	[SerializeField] private string partName;
	[SerializeField] private Button button;
	private AircraftShipPart part;
	private DamageControlUI ui;

	public void Initialize(DamageControlUI ui, ShipPartBridge bridge)
	{
		this.ui = ui;
		part = bridge.parts.FirstOrDefault(p => p.name == partName);
		button.onClick.AddListener(OnClick);
	}
	
	private void OnClick()
	{
		ui.SelectPart(part);
	}

	public void ChangeColor(Color color)
	{
		button.image?.color = color;
	} 
}