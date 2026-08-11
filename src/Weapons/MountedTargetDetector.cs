using UnityEngine;

namespace NOComponentWIP;

public class MountedTargetDetector : TargetDetector
{
	public override void Awake()
	{
		if (attachedUnit == null) return;
		base.Awake();
	}

	public void AttachToUnit(Unit unit, UnitPart part)
	{
		SetAttachedUnit(unit);
		this.part = part;
		attachedUnit.onInitialize -= TargetDetector_OnInitialize;
		TargetDetector_OnInitialize();
		
		part.onApplyDamage -= TargetDetector_OnApplyDamage;
		part.onApplyDamage += TargetDetector_OnApplyDamage;

		attachedUnit.onDisableUnit -= TargetDetector_OnUnitDisabled;
		attachedUnit.onDisableUnit += TargetDetector_OnUnitDisabled;
	}
}