using UnityEngine;
using UnityEngine.UI;

namespace NOComponentWIP;

public class PitchGauge : CustomAxis1Gauge
{
	public override void Refresh()
	{
		var axis = (inputs.pitch + 1f) / 2;
		
		if (!(aircraft == null) && axisPrev != axis)
		{
			axisPrev = axis;
			float num = axisSplitPosition + idleZone;
			float num2 = axisSplitPosition - idleZone;
			float fillAmount = (axis - num) / (1f - num);
			float fillAmount2 = (num2 - axis) / num2;
			positiveBar.fillAmount = fillAmount;
			negativeBar.fillAmount = fillAmount2;
		}
	}
}