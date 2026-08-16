using System;

namespace NOComponentWIP;

public class VLSBoosterActive : VLSBooster
{
	private new void Awake()
	{
		missile.onInitialize += VLSBoosterActive_OnInitialize;
		burnRate = fuelMass / burnTime;
	}

	private void VLSBoosterActive_OnInitialize()
	{
		missile.onInitialize -= VLSBoosterActive_OnInitialize;
		if (missile.owner == null || GameManager.gameState == GameState.Encyclopedia)
		{
			missile.boosterIsAttached = false;
			Destroy(gameObject);
		}
		else
		{
			missile.boosterIsAttached = true;
		}
	}
}