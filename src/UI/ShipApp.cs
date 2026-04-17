using UnityEngine;

namespace NOComponentWIP;

public class ShipApp : MonoBehaviour
{
	[SerializeField] protected float fontSizeMultiplier = 1f;

	protected int fontSize;
	protected Color fontColor;

	public virtual void Initialize(Aircraft aircraft, ShipPartBridge bridge)
	{
		
	}

	public virtual void Refresh()
	{
		
	}
	
	public virtual void RefreshSettings()
	{
		fontSize = (int)PlayerSettings.hudTextSize;
	}
}