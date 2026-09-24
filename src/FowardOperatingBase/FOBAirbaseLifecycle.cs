using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NOComponentWIP;

internal sealed class FOBAirbaseLifecycle : MonoBehaviour
{
    private Airbase _airbase;
    private Unit _centerUnit;
    private List<Unit> _members;
    private bool _tearingDown;
    
    // To-do: Check what happens when someone has a FOB's AircraftSelectionMenu open as its center gets destroyed and FOB
    // gets cleaned up? It might not fire ReturnToMap() as usual, in which case the menu can briefly break, until
    // they exit back to map
    
    private void OnDestroy()
    {
        Unsubscribe();
    }
    
    public static void Attach(Airbase airbase, Unit centerUnit, IEnumerable<Unit> members)
    {
        if (airbase == null || centerUnit == null) return;
        if (airbase.TryGetComponent<FOBAirbaseLifecycle>(out _)) return;
        
        var lifecycle = airbase.gameObject.AddComponent<FOBAirbaseLifecycle>();
        lifecycle._airbase = airbase;
        lifecycle._centerUnit = centerUnit;
        lifecycle._members = members?.Where(unit => unit != null).Distinct().ToList() ?? [];
        
        centerUnit.onDisableUnit += lifecycle.CenterDisabled;
        
        // In case for any reason the center is disabled before lifecycle listener is subscribed
        if (centerUnit.disabled)
            lifecycle.CenterDisabled(centerUnit);
    }
    
    private void CenterDisabled(Unit unit)
    {
        if (_tearingDown) return;
        _tearingDown = true;
        Unsubscribe();
        FOBManager.RemoveFOBAirbase(_airbase, _members);
    }
    
    private void Unsubscribe()
    {
        if (_centerUnit == null) return;
        _centerUnit.onDisableUnit -= CenterDisabled;
        _centerUnit = null;
    }
}