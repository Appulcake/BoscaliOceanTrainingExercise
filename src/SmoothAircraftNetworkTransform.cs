using NuclearOption.NetworkTransforms;
using UnityEngine;

namespace NOComponentWIP;

public class SmoothAircraftNetworkTransform : AircraftNetworkTransform
{
	public NetworkPIDSmoother networkSmoother;
	private Rigidbody initializedRb;

	public override void Awake()
	{
		base.Awake();
	}
	
	private bool CheckSmootherInitialized(Rigidbody rb)
	{
		if (rb == null || networkSmoother == null) return false;
		if (initializedRb == rb) return true;
		networkSmoother.Initialize(rb);
		initializedRb = rb;
		return true;
	}

	public override void VisualUpdate(ref VisualUpdateTime visualTime)
	{
		var validatedAircraft = Aircraft;
		if (validatedAircraft == null)
			return;
		
		if (base.HasAuthority || validatedAircraft.LocalSim)
		{
			initializedRb = null;
			return;
		}
		
		var rb = validatedAircraft.rb;
		if (rb == null || rb.isKinematic)
		{
			initializedRb = null;
			return;
		}
		
		using (visualUpdateMarker.Auto())
		{
			if (!TryGetSnapshot(ref visualTime, out var snapshot) || !CheckSmootherInitialized(rb))
				return;
			
			networkSmoother.SmoothRB(rb, snapshot);
			validatedAircraft.CheckSpawnedInPosition();
		}
	}
}