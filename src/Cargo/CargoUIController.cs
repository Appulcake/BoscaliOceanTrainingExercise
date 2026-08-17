using System.Collections.Generic;
using NOComponentWIP.ServerConfig;
using NuclearOption.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NOComponentWIP;

public class CargoUIController : MonoBehaviour
{
	[Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image pointsFillBar;

    [Header("List Area")]
    [SerializeField] private Transform scrollContent;
    
    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Toggle fobToggle;
    [SerializeField] private GameObject fobRow;

    private DeploymentManager manager;
    private Dictionary<DeployableUnit, int> manifest = new();
    private int currentTotalPoints = 0;

    public void Initialize(DeploymentManager manager)
    {
        this.manager = manager;
        if (manager == null)
        {
            Plugin.Logger.LogError("CargoUIController manager is null");
            return;
        }
        
        applyButton.onClick.RemoveAllListeners();
        applyButton.onClick.AddListener(OnApplyClicked);
        
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Clear);
        
        fobToggle.onValueChanged.RemoveAllListeners();
        fobToggle.onValueChanged.AddListener(ToggleFOB);
        
        bool fobAvailable = manager.FobAvailable;
        if (fobRow != null) fobRow.SetActive(fobAvailable);
        fobToggle.interactable = fobAvailable;
        fobToggle.isOn = fobAvailable && LoadoutBridge.IncludeFOB;
        
        //foreach (Transform child in scrollContent) Destroy(child.gameObject);
        
        manifest.Clear();
        
        foreach (var kvp in LoadoutBridge.Manifest)
        {
            if (kvp.Key != null && kvp.Value > 0)
            {
                manifest[kvp.Key] = kvp.Value;
            }
        }
        foreach (var unit in manager.availableUnits)
        {
            if (unit == null) continue;
            if (unit.eventContent && !MissionManager.AllowEventContent) continue;
            if (!UnitConfig.UnitAllowed(unit.JsonKey)) continue;
            
            var rowObj = Instantiate(ModAssets.i.CargoEditorRow, scrollContent);
            var rowController = rowObj.GetComponent<UnitRowController>();
            
            if (rowController == null) continue;
            
            manifest.TryGetValue(unit, out int currentCount);
            rowController.Setup(unit, currentCount, this);
        }

        RefreshInfo();
        UpdateButtons();
    }

    private void ToggleFOB(bool toggle)
    {
        RefreshInfo();
        UpdateButtons();
    }

    public void ChangeUnitCount(DeployableUnit unit, int delta, int unitCost, out int deltaActual)
    {
        deltaActual = 0;
        if (unit == null) return;

        manifest.TryGetValue(unit, out int current);
        int nextCount = Mathf.Max(0, current + delta);

        if (delta > 0 && (currentTotalPoints + unitCost > manager.MaxPoints))
        {
            return;
        }

        deltaActual = delta;

        if (nextCount > 0)
        {
            manifest[unit] = nextCount;
        }
        else
        {
            manifest.Remove(unit);
        }

        RefreshInfo();
        UpdateButtons();
    }

    private void RefreshInfo()
    {
        currentTotalPoints = (fobToggle.isOn && manager.FobAvailable) ? manager.FobCost : 0;
        foreach (var entry in manifest)
        {
            if (entry.Key != null)
            {
                currentTotalPoints += entry.Value * entry.Key.pointCost;
            }
        }

        if (pointsText != null)
        {
            pointsText.text = $"CAPACITY: {currentTotalPoints} / {manager.MaxPoints}";
        }

        if (pointsFillBar != null)
        {
            float fillRatio = manager.MaxPoints > 0 ? (float)currentTotalPoints / manager.MaxPoints : 0f;
            pointsFillBar.fillAmount = Mathf.Clamp01(fillRatio);
            pointsFillBar.color = currentTotalPoints > (manager.MaxPoints * 0.9f) ? Color.red : Color.green;
        }

        if (costText != null)
        {
            if (UnitConfig.UnitEconomy())
            {
                costText.text = $"COST: {LoadoutBridge.CalculateCost(manifest)}m";
            }
            else
            {
                costText.text = "COST: DISABLED";
                costText.color = Color.green;
            }
            
        }
    }

    private void UpdateButtons()
    {
        if (manager == null) return;

        int remainingPoints = manager.MaxPoints - currentTotalPoints;
        
        if (manager.FobAvailable)
        {
            fobToggle.interactable = fobToggle.isOn || (remainingPoints >= manager.FobCost);
        }
        else
        {
            fobToggle.interactable = false;
        }

        if (GameManager.GetLocalPlayer(out Player player) && player.Allocation < LoadoutBridge.CalculateCost(manifest) && UnitConfig.UnitEconomy())
        {
            costText.color = Color.red;
        }
        else
        {
            costText.color = Color.white;
        }

        var rows = scrollContent.GetComponentsInChildren<UnitRowController>();
        foreach (var row in rows)
        {
            if (row != null)
            {
                row.UpdateAbilityToIncrement(currentTotalPoints, manager.MaxPoints);
            }
        }
    }

    private void OnApplyClicked()
    {
        Dictionary<DeployableUnit, int> finalManifest = new();
        foreach (var entry in manifest)
        {
            finalManifest.Add(entry.Key, entry.Value);
        }
        
        LoadoutBridge.SetLoadout(finalManifest, fobToggle.isOn);
        
        Close();
    }

    private void Clear()
    {
        LoadoutBridge.Clear();
        Close();
    }
    
    public void Close()
    {
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        applyButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.RemoveAllListeners();
        fobToggle?.onValueChanged.RemoveAllListeners();
    }
}