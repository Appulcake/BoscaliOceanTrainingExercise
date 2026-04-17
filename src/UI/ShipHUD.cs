using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using NOComponentWIP.Patches;

namespace NOComponentWIP;

public class ShipHUD : HUDApp
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentParent;
    [SerializeField] private Text itemTemplate; 
    [SerializeField] private float itemHeight = 31f;
    [SerializeField] private Text fobStatus;
    [SerializeField] private GameObject deploymentHud;
    [SerializeField] private Text resupplyText;
    [SerializeField] private Text disembarkText;
    
    private Aircraft aircraft;
    private DeploymentManager manager;
    private ShipPartBridge bridge;
    private FOBManager fobManager;
    private ResupplyController resupplyController;
    private List<Text> pool = new List<Text>();
    private float lastDisembarkRefresh;

    public override void Initialize(Aircraft aircraft)
    {
        this.aircraft = aircraft;
        bridge = aircraft.GetComponent<ShipPartBridge>();
        
        manager = bridge.deploymentManager;
        fobManager = bridge.fobManager;
        resupplyController = bridge.resupplyController;
        
        if (bridge == null)
        {
            gameObject.SetActive(false);
            return;
        }

        itemTemplate.gameObject.SetActive(false);
        
        lastDisembarkRefresh = 0f;
}

    public override void Refresh()
    {
        DisembarkCheck();
        
        bool hasFobSystem = fobManager != null;
        if (fobStatus.gameObject.activeSelf != hasFobSystem)
            fobStatus.gameObject.SetActive(hasFobSystem);

        if (hasFobSystem)
        {
            if (fobManager.hasFob)
            {
                fobStatus.text = "FOB: READY";
                fobStatus.color = (manager != null && manager.FobSelected) ? Color.green : Color.cyan;
            }
            else
            {
                fobStatus.text = "FOB: EMPTY";
                fobStatus.color = Color.red;
            }
        }
        
        bool hasResupply = resupplyController != null;
        if (resupplyText.gameObject.activeSelf != hasResupply)
            resupplyText.gameObject.SetActive(hasResupply);

        if (hasResupply)
        {
            if (resupplyController.ResupplyDistance > 0f)
            {
                resupplyText.text = $"RESUPPLY: INBOUND - {UnitConverter.DistanceReading(resupplyController.ResupplyDistance)}";
            } else if (resupplyController.ResupplyCalled)
            {
                resupplyText.text = $"RESUPPLY: CALLED";
            }
            else
            {
                resupplyText.text = $"RESUPPLY: READY";
            }
        }
        
        bool hasManager = manager != null;
        if (deploymentHud.activeSelf != hasManager)
            deploymentHud.SetActive(hasManager);
        
        if (!hasManager) 
        {
            UpdatePool(0);
            return;
        }
        
        if (manager.IsEmpty())
        {
            UpdatePool(1); 
            pool[0].text = "EMPTY";
            pool[0].color = !manager.FobSelected ? Color.red : new Color(1, 1, 1, 0.4f);
        
            contentParent.anchoredPosition = Vector2.zero;
            return;
        }
        
        var uniqueTypes = GetUniqueManifestTypes();
        UpdatePool(uniqueTypes.Count);

        int visualSelectedIndex = 0;
        int currentSelectedID = manager.unitManifest[manager.SelectedIndex];

        for (int i = 0; i < uniqueTypes.Count; i++)
        {
            int typeID = uniqueTypes[i];
            int count = manager.unitManifest.Count(id => id == typeID);
        
            pool[i].text = $"{manager.availableUnits[typeID].unitName} x{count}";
        
            if (typeID == currentSelectedID && !manager.FobSelected)
            {
                pool[i].color = Color.green;
                visualSelectedIndex = i;
            }
            else
            {
                pool[i].color = new Color(1, 1, 1, 0.4f);
            }
        }
        
        float totalHeight = uniqueTypes.Count * itemHeight;
        float itemLocalY = (totalHeight / 2f) - (visualSelectedIndex * itemHeight) - (itemHeight / 2f);
        float targetY = -itemLocalY;

        Vector2 anchoredPos = contentParent.anchoredPosition;
        anchoredPos.y = Mathf.Lerp(anchoredPos.y, targetY, Time.deltaTime * 10f);
        contentParent.anchoredPosition = anchoredPos;

        
    }

    private void DisembarkCheck()
    {
        if (Time.timeSinceLevelLoad > lastDisembarkRefresh + 1f)
        {
            lastDisembarkRefresh = Time.timeSinceLevelLoad;
            var ab = aircraft.GetComponent<Airbase>();
            bool range = aircraft.NetworkHQ.AnyNearAirbaseInRange(aircraft.transform.position, out _, 2000f, ab);
            bool speed = aircraft.speed < 10f;
            
            if (range && speed)
            {
                disembarkText.text = "DISEMBARK: SAFE";
                disembarkText.color = Color.green;
            } else if (range)
            {
                disembarkText.text = "DISEMBARK: SPEED";
                disembarkText.color = Color.yellow;
            }
            else
            {
                disembarkText.text = "DISEMBARK: RANGE";
                disembarkText.color = Color.red;
            }
        }
    }

    private List<int> GetUniqueManifestTypes()
    {
        if (manager == null) return new List<int>();

        List<int> unique = new List<int>();
        foreach (int id in manager.unitManifest)
        {
            if (!unique.Contains(id)) unique.Add(id);
        }
        return unique;
    }

    private void UpdatePool(int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            Text newItem = Instantiate(itemTemplate, contentParent);
            newItem.gameObject.SetActive(true);
            pool.Add(newItem);
        }
        while (pool.Count > requiredCount)
        {
            if (pool[pool.Count - 1] != null) 
                Destroy(pool[pool.Count - 1].gameObject);
            pool.RemoveAt(pool.Count - 1);
        }
    }
}