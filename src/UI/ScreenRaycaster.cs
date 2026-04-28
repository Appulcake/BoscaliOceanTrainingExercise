using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NOComponentWIP;

public class ScreenRaycaster : GraphicRaycaster
{
	public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
	{
		var cgroup = FlightHud.i.gameObject.GetComponent<CanvasGroup>();
		//cgroup.blocksRaycasts = false;
		
		eventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
		base.Raycast(eventData, resultAppendList);
		//cgroup.blocksRaycasts = true;
	}
}