using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingItemUI : MonoBehaviour
{
    public WeaponID WeaponID;
    public string ItemName;
    public Image ItemIcon;

    public int MetalBarsNeeded = 0;
    public int CrystalOresNeeded = 0;
    private int DefaultMetalBarsCost, DefaultCrystalOresCost;

    void Start()
    {
        DefaultCrystalOresCost = CrystalOresNeeded;
        DefaultMetalBarsCost = MetalBarsNeeded;

        TeamInventory.OnCraftingCostReductionChanged += UpdateCost;
    }

    void UpdateCost()
    {
        MetalBarsNeeded = (int)(DefaultMetalBarsCost * (1.0f - TeamInventory.CRAFTING_COST_REDUCTION_PERC));
    }


}
