using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class WorkshopManager : MonoBehaviour
{
    #region Singleton
    public static WorkshopManager instance = null;
    #endregion;

    // Crafting UI
    public Text OwnedMetalBars, OwnedCrystalOres;
    public Text CraftingItemName;
    public Image CraftingItemImage;
    public Text MetalBarCost, CrystalOreCost;
    public Button CraftButton;

    // Team Inevntory UI
    public RectTransform itemRect;
    public GameObject InventoryItemUIPrefab;
    public GameObject InventoryItemDetailPanel;
    public GameObject InventoryNoItemPanel;
    public Text InventoryItemName;
    public Image InventoryItemImage;
    public Text InventoryWeaponDurability;
    public Image InventoryDurabilityFill;

    // Player Weapons UI
    public Image[] PlayerWeaponIcon;
    public Image[] PlayerWeaponDurabilityFill;
   

    private Weapon selected_weapon = null;
    public CraftingItemUI selected_item_to_craft = null;
    private int current_index = 0;


    public static bool IsWorkshopOpen = false;

    private float timer = 0.0f;

    private static PlayerController playerController = null;
    private static PlayerWeapons playerWeapons = null;

    void Start()
    {
        instance = this;
        StartCoroutine(WorkshopInits());
    }

    void Update()
    {
        if (IsWorkshopOpen && Input.GetKeyDown(GameSettings.TOGGLE_WORKSHOP_KEY))
            ToggleWorkshop(false);

        if (timer > 0.0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0.0f)
            {
                timer = 0.0f;
                CraftButton.interactable = true;
            }
        }
    }
    IEnumerator WorkshopInits()
    {
        TeamInventory.OnOwnedMaterialsChanged += UpdateOwnedMaterialsUI;
        TeamInventory.OnOwnedMaterialsChanged += UpdateCraftingItemUI;
        TeamInventory.OnInventoryUpdated += RefreshTeamInventoryRect;
        PlayerWeapons.OnWeaponsChanged += UpdatePlayerWeaponsUI;
        RefreshTeamInventoryRect();
        UpdateOwnedMaterialsUI();
        UpdateCraftingItemUI();

        while (PlayerData.myPlayer == null)
            yield return null;
        playerController = PlayerData.myPlayer.gameObject.GetComponent<PlayerController>();
        playerWeapons = PlayerData.myPlayer.gameObject.GetComponent<PlayerWeapons>();
    }

    public void ToggleWorkshop(bool open)
    {
        StartCoroutine(ToggleWorkshopWait(open));
    }
    IEnumerator ToggleWorkshopWait(bool open)
    {
        yield return new WaitForEndOfFrame();
        playerController.LOCK_PLAYER_MOVEMENT = !playerController.LOCK_PLAYER_MOVEMENT;
        UIManager.instance.ToggleWorkshopUI(open);
        if (IsWorkshopOpen)
        {
            UpdateInventoryItemUI();
            UpdateCraftingItemUI();
            UpdatePlayerWeaponsUI(0);
            UpdatePlayerWeaponsUI(1);
        }
    }

    public void CraftWeapon()
    {
        if (selected_item_to_craft == null)
            return;

        if (TeamInventory.TeamMetalBars < selected_item_to_craft.MetalBarsNeeded || 
            TeamInventory.TeamCrystalOres < selected_item_to_craft.CrystalOresNeeded)
        {
            // TBC - Display text maybe?
            return;
        }

        CraftButton.interactable = false;
        timer = 0.5f;

        PrefabId weaponID = BoltPrefabs.Melee;

        switch (selected_item_to_craft.WeaponID)
        {
            case WeaponID.FRYING_PAN:
                weaponID = BoltPrefabs.FryingPan;
                break;
            case WeaponID.EGG_LAUNCHER:
                weaponID = BoltPrefabs.EggLauncher;
                break;
            default:
                break;
        }
        SendCraftWeaponEvent(weaponID, selected_item_to_craft.WeaponID);
        SendUpdateTeamMaterialsEvent(-1 * selected_item_to_craft.MetalBarsNeeded, -1 * selected_item_to_craft.CrystalOresNeeded);
        

    }

    public void EquipWeapon()
    {
        PlaceWeaponIntoInventory();

        PickUpWeaponEvent evnt = PickUpWeaponEvent.Create(GlobalTargets.OnlyServer);
        evnt.WeaponEntity = selected_weapon.entity;
        evnt.Player = PlayerData.myPlayer;
        evnt.Index = current_index;
        evnt.Send();
        selected_weapon.gameObject.SetActive(true);
        RemoveItemFromInventory(selected_weapon.entity);
        selected_weapon = null;
        UpdateInventoryItemUI();
    }

    public void PlaceWeaponIntoInventory()
    {
        Weapon currWeapon = playerWeapons.GetWeapon(current_index);
        BoltEntity weaponEntity = currWeapon.GetComponent<BoltEntity>();
        if (weaponEntity != null && currWeapon.ID != WeaponID.MELEE)
        {
            StartCoroutine(WaitWeaponDetach(current_index, weaponEntity));
        }
    }

    IEnumerator WaitWeaponDetach(int index, BoltEntity weaponEntity)
    {
        DropWeaponEvent evnt = DropWeaponEvent.Create(GlobalTargets.OnlyServer);
        evnt.Player = PlayerData.myPlayer;
        evnt.Index = index;
        evnt.Send();

        Weapon weapon = weaponEntity.gameObject.GetComponent<Weapon>();
        while (!weapon.is_stray)
        {
            yield return null;
        }
        PlaceWeaponIntoInventory(weaponEntity);
    }

    public void PlaceWeaponIntoInventory(BoltEntity weaponEntity)
    {
        AddToTeamInventoryEvent add_evnt = AddToTeamInventoryEvent.Create(GlobalTargets.Everyone);
        add_evnt.Team = PlayerData.myPlayer.GetState<IPlayerState>().Team;
        add_evnt.WeaponEntity = weaponEntity;
        add_evnt.PlayerName = PlayerData.myPlayer.GetState<IPlayerState>().Name;
        add_evnt.Send();
    }

    void RemoveItemFromInventory(BoltEntity weapon)
    {
        TakeFromTeamInventoryEvent evnt = TakeFromTeamInventoryEvent.Create(GlobalTargets.Everyone);
        evnt.Weapon = weapon;
        evnt.Team = PlayerData.myPlayer.GetState<IPlayerState>().Team;
        evnt.PlayerName = PlayerData.myPlayer.GetState<IPlayerState>().Name;
        evnt.Send();
    }

    public void SetSelectedWeapon(InventoryItemUI item)
    {
        selected_weapon = item.GetWeapon();
        UpdateInventoryItemUI();
    }

    public void SetSelectedCraftItem(CraftingItemUI item)
    {
        selected_item_to_craft = item;
        UpdateCraftingItemUI();
    }

    public void SetCurrentIndex(int index)
    {
        current_index = index;
    }

    void UpdateCraftingItemUI()
    {
        CraftingItemName.text = selected_item_to_craft.ItemName;
        CraftingItemImage.sprite = selected_item_to_craft.ItemIcon.sprite;
        MetalBarCost.text = selected_item_to_craft.MetalBarsNeeded.ToString();
        CrystalOreCost.text = selected_item_to_craft.CrystalOresNeeded.ToString();
        if (TeamInventory.TeamMetalBars < selected_item_to_craft.MetalBarsNeeded)
            MetalBarCost.color = Color.red;
        else
            MetalBarCost.color = Color.white;
        if (TeamInventory.TeamCrystalOres < selected_item_to_craft.CrystalOresNeeded)
            CrystalOreCost.color = Color.red;
        else
            CrystalOreCost.color = Color.white;
    }

    void RefreshTeamInventoryRect()
    {
        // Clear
        foreach (Transform child in itemRect)
        {
            Destroy(child.gameObject);
        }

        int numWeapons = TeamInventory.GetWeaponsCount();
        for (int i = 0; i < numWeapons; i++)
        {
            GameObject go = Instantiate(InventoryItemUIPrefab, itemRect, false);
            InventoryItemUI item = go.GetComponent<InventoryItemUI>();
            item.Init(TeamInventory.GetWeapon(i), SetSelectedWeapon);
        }
        for (int i = numWeapons; i < TeamInventory.MAX_WEAPONS; i++)
        {
            GameObject go = Instantiate(InventoryItemUIPrefab, itemRect, false);
            InventoryItemUI item = go.GetComponent<InventoryItemUI>();
            item.InitEmpty(SetSelectedWeapon);
        }

    }

    void UpdateInventoryItemUI()
    {
        if (selected_weapon == null)
        {
            InventoryNoItemPanel.gameObject.SetActive(true);
            InventoryItemDetailPanel.gameObject.SetActive(false);
            return;
        }
        InventoryNoItemPanel.gameObject.SetActive(false);
        InventoryItemDetailPanel.gameObject.SetActive(true);
        InventoryItemName.text = selected_weapon.GetName();
        InventoryItemImage.sprite = selected_weapon.weaponIconSprite;
        InventoryDurabilityFill.fillAmount = selected_weapon.state.Durability / selected_weapon.state.MaxDurability;
        InventoryWeaponDurability.text = selected_weapon.state.Durability + "/" + selected_weapon.state.MaxDurability;
        if (InventoryDurabilityFill.fillAmount < 0.2f)
            InventoryDurabilityFill.color = new Color32(255, 150, 150, 255);
        else
            InventoryDurabilityFill.color = Color.white;
    }

    void UpdateOwnedMaterialsUI()
    {
        OwnedCrystalOres.text = TeamInventory.TeamCrystalOres.ToString();
        OwnedMetalBars.text = TeamInventory.TeamMetalBars.ToString();
    }

    void UpdatePlayerWeaponsUI(int index)
    {
        if (playerWeapons == null)
        {
            return;
        }
        Weapon weapon = playerWeapons.GetWeapon(index);
        if (weapon.ID == WeaponID.MELEE)
        {
            PlayerWeaponDurabilityFill[index].gameObject.SetActive(false);
        }
        else
        {
            PlayerWeaponDurabilityFill[index].gameObject.SetActive(true);
            PlayerWeaponDurabilityFill[index].fillAmount = weapon.GetDurability() / Weapon.MAX_DURABILITY;
        }
        PlayerWeaponIcon[index].sprite = weapon.weaponIconSprite;
      
    }

    void SendUpdateTeamMaterialsEvent(int dMetal, int dCrystal)
    {
        UpdateTeamMaterialsEvent evnt = UpdateTeamMaterialsEvent.Create(GlobalTargets.Everyone);
        evnt.Team = PlayerData.myPlayer.GetState<IPlayerState>().Team;
        evnt.DeltaMetalBars = dMetal;
        evnt.DeltaCrystalOres = dCrystal;
        evnt.Send();
    }

    void SendCraftWeaponEvent(PrefabId to_craft_id, WeaponID weapon_id)
    {
        CraftWeaponEvent evnt = CraftWeaponEvent.Create(GlobalTargets.Everyone);
        evnt.WeaponPrefabID = to_craft_id;
        evnt.WeaponID = (int)weapon_id;
        evnt.Team = PlayerData.myPlayer.GetState<IPlayerState>().Team;
        evnt.Durability = Weapon.MAX_DURABILITY;
        evnt.PlayerName = PlayerData.myPlayer.GetState<IPlayerState>().Name;
        evnt.Send();
    }
}
