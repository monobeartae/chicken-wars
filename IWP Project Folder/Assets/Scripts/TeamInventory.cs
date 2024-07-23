using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Bolt;

public class TeamInventory 
{
    public static int MAX_WEAPONS = 15;
    private static List<Weapon> inventory = new List<Weapon>();

    // Local Team Stats
    public static int TeamMetalBars = 0;
    public static int TeamCrystalOres = 0;
    public static float WEAPON_DURABILITY_MULTIPLIER = 1.0f;
    public static float WEAPON_DAMAGE_MULTIPLIER = 1.0f;
    public static float CRAFTING_COST_REDUCTION_PERC = 0.0f;
    public static float CRAFTING_LUCK = 0.0f;
    public static float MATERIALS_GATHERED_MULTIPLIER = 1.0f;

    public delegate void OwnedMaterialsChanged();
    public static event OwnedMaterialsChanged OnOwnedMaterialsChanged;

    public delegate void WeaponDurabilityChanged(float delta_perc);
    public static event WeaponDurabilityChanged OnWeaponDurabilityChanged;

    public delegate void InventoryUpdated();
    public static event InventoryUpdated OnInventoryUpdated;

    public delegate void CraftingCostReductionChanged();
    public static event CraftingCostReductionChanged OnCraftingCostReductionChanged;

    public static void AddToInventory(Weapon weapon)
    {
        inventory.Add(weapon);
        weapon.gameObject.SetActive(false);
        SortList();
        OnInventoryUpdated?.Invoke();
    }

    public static void RemoveFromInventory(Weapon weapon)
    {
        inventory.Remove(weapon);
        SortList();
        OnInventoryUpdated?.Invoke();
    }

    static void SortList()
    {
        inventory.Sort(SortByDurability);
    }

    public static void UpdateMetalBarsOwned(int delta_amount)
    {
        TeamMetalBars += (int)(delta_amount * MATERIALS_GATHERED_MULTIPLIER);
        OnOwnedMaterialsChanged.Invoke();
        NotificationsManager.instance.AddCurrencyEarnedUI(CURRENCY_TYPE.METAL_BARS, (int)(delta_amount * MATERIALS_GATHERED_MULTIPLIER));
    }
    public static void UpdateCrystalOresOwned(int delta_amount)
    {
        TeamCrystalOres += (int)(delta_amount * MATERIALS_GATHERED_MULTIPLIER);
        OnOwnedMaterialsChanged.Invoke();
        NotificationsManager.instance.AddCurrencyEarnedUI(CURRENCY_TYPE.CRYSTAL_ORES, (int)(delta_amount * MATERIALS_GATHERED_MULTIPLIER));
    }

    public static void UpdateWeaponDurability(float delta_perc)
    {
        WEAPON_DURABILITY_MULTIPLIER *= (1.0f + delta_perc);
        Weapon.MAX_DURABILITY *= (1.0f + delta_perc);
        OnWeaponDurabilityChanged?.Invoke(delta_perc);
    }
    public static void UpdateWeaponDamageMultiplier(float delta_perc)
    {
        WEAPON_DAMAGE_MULTIPLIER *= (1.0f + delta_perc);
    }

    public static void UpdateCraftingCostReduction(float delta_perc)
    {
        CRAFTING_COST_REDUCTION_PERC += delta_perc;
        OnCraftingCostReductionChanged?.Invoke();
    }

    public static void UpdateCraftingLuck(float delta_perc)
    {
        CRAFTING_LUCK += delta_perc;
    }

    public static void UpdateMaterialsGatheredMultiplier(float delta_perc)
    {
        MATERIALS_GATHERED_MULTIPLIER *= (1.0f + delta_perc);
    }

    public static int GetWeaponsCount()
    {
        return inventory.Count;
    }

    public static Weapon GetWeapon(int index)
    {
        return inventory[index];
    }
    static int SortByDurability(Weapon w1, Weapon w2)
    {
        return w2.GetMaxDurability().CompareTo(w1.GetMaxDurability());
    }
}
