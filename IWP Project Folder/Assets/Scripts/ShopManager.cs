using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    #region Singleton
    public static ShopManager instance = null;
    #endregion

    public Text itemName, itemDesc, itemCost;
    public Button unlockButton;
    public Text unlockStatusText;
    public Text playerCoins;

    public ShopItemUI selected_item = null;

    public GameObject UtilityItems, TeamItems, PlayerItems;

    public static bool IsShopOpen = false;

    private Vector3 zAxis = new Vector3(0.0f, 0.0f, 1.0f);
    private static PlayerController playerController = null;

    public List<ShopItemUI> AllItemsList = new List<ShopItemUI>();



    void Start()
    {
        instance = this;
        UpdateItemDescUI();
        StartCoroutine(InitShopManager());
    }

    IEnumerator InitShopManager()
    {

        while (UIManager.instance == null)
            yield return null;

        UIManager.instance.ToggleShopUI(true);

        PlayerData.OnPlayerCoinsChanged += UpdatePlayerCoinsUI;
        UpdatePlayerCoinsUI();

        while (PlayerData.myPlayer == null)
            yield return null;

        playerController = PlayerData.myPlayer.gameObject.GetComponent<PlayerController>();

        while (!isShopItemsInitComplete())
            yield return null;

        TeamItems.SetActive(false);
        PlayerItems.SetActive(false);
        UIManager.instance.ToggleShopUI(false);
    }

    private bool isShopItemsInitComplete()
    {
        foreach (ShopItemUI item in AllItemsList)
        {
            if (!item.isComplete)
                return false;
        }
        return true;
    }

    public void ToggleShop()
    {
        playerController.LOCK_PLAYER_MOVEMENT = !playerController.LOCK_PLAYER_MOVEMENT;
        UIManager.instance.ToggleShopUI();
        UpdateItemDescUI();
    }

    public void SetSelectedShopitem(ShopItemUI shop_item)
    {
        // Reset Rotation of Previous Selection
        selected_item.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        // Set New Item Var and Update Description UI
        selected_item = shop_item;
        UpdateItemDescUI();
    }

    void Update()
    {
        if (!IsShopOpen)
            return;

        float SPIN_SPEED = 15.0f;
        float theta = SPIN_SPEED * Time.deltaTime;
        selected_item.gameObject.transform.Rotate(zAxis, theta);
    }

    void UpdatePlayerCoinsUI()
    {
        playerCoins.text = PlayerData.PlayerCoins.ToString();
    }

    void UpdateItemDescUI()
    {
        itemName.text = selected_item.ItemName.Replace("\\n", "\n");
        itemDesc.text = selected_item.ItemDesc.Replace("\\n", "\n");
        int item_cost = selected_item.ItemCost;
        itemCost.text = item_cost.ToString();
        if (!selected_item.isUnlockable()) // not unlockable
        {
            unlockButton.gameObject.SetActive(false);
            unlockStatusText.gameObject.SetActive(true);
            unlockStatusText.text = "Unlock the parent node first!";
        }
        else if (!selected_item.GetIsUnlocked()) // Not Unlocked
        {
            unlockButton.gameObject.SetActive(true);
            unlockStatusText.gameObject.SetActive(false);
            if (PlayerData.PlayerCoins < item_cost)
                itemCost.color = Color.red;
            else
                itemCost.color = Color.white;
        }
        else // Unlocked
        {
            unlockButton.gameObject.SetActive(false);
            unlockStatusText.gameObject.SetActive(true);
            unlockStatusText.text = "Unlocked.";
        }
    }

    public void UnlockItem()
    {
        UnlockItem(selected_item);
    }

    public void UnlockItem(ShopItemUI itemUI)
    {
        // Check if enough cost
        if (PlayerData.PlayerCoins < selected_item.ItemCost)
        {
            // TBC - Warning Message??
            return;
        }

        PlayerData.UpdatePlayerCoins(-1 * selected_item.ItemCost);
        selected_item.UnlockItem();
        UpdateItemDescUI();
    }

    public ShopItemUI Find(ITEM_TYPE item_id)
    {
        return AllItemsList.Find(s => s.ItemID == item_id);
    }
}
