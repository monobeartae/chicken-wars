using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class ShopItemUI : MonoBehaviour
{
    public string ItemName, ItemDesc;
    public int ItemCost = 0;
    public Image ItemIcon;
    public ITEM_TYPE ItemID = ITEM_TYPE.UNSET;


    public ShopItemUI ParentItem = null;
    public Image[] ChildLinks = null;

    private bool is_unlocked = false;

    private CanvasGroup IconCanvasGroup;

    public delegate void Unlock();
    public Unlock OnUnlock;

    public bool isComplete = false;

    public void Start()
    {
        StartCoroutine(ShopItemInit());
    }

    IEnumerator ShopItemInit()
    {
        while (PlayerData.myPlayer == null)
            yield return null;


        OnUnlock += UnlockAction;

        foreach (Image childlink in ChildLinks)
        {
            childlink.color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        }

        IconCanvasGroup = GetComponent<CanvasGroup>();
        IconCanvasGroup.alpha = 0.2f;

        isComplete = true;
    }

    private void UnlockAction()
    {
        PlayerData playerData = PlayerData.myPlayer.gameObject.GetComponent<PlayerData>();
        UpgradePlayerStatsEvent evnt = null;
        switch (ItemID)
        {
            case ITEM_TYPE.LANTERN_UPGRADE:
                PlayerData.myPlayer.gameObject.GetComponent<PlayerLantern>().UpgradeLanternLight();
                break;
            case ITEM_TYPE.PLAYER_SPEED_0:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.05f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_SPEED_1:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.1f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_SPEED_2:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.15f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.WEAPON_UPGRADE_0:
            case ITEM_TYPE.WEAPON_UPGRADE_1:
            case ITEM_TYPE.WEAPON_UPGRADE_21:
            case ITEM_TYPE.WEAPON_UPGRADE_22:
            case ITEM_TYPE.CRAFTING_UPGRADE_0:
            case ITEM_TYPE.CRAFTING_UPGRADE_10:
            case ITEM_TYPE.CRAFTING_UPGRADE_11:
                SendTeamUpgradeEvent(playerData.state.Team, (int)ItemID);
                break;
            case ITEM_TYPE.PLAYER_HP_0:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.2f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_HP_1:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.35f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_HP_2:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.5f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_OFFENSE_0:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 5;
                evnt.DeltaAttackSpeed = 0.15f;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_OFFENSE_1:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 10;
                evnt.DeltaAttackSpeed = 0.20f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_OFFENSE_20:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 30;
                evnt.DeltaAttackSpeed = 0.0f;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            case ITEM_TYPE.PLAYER_OFFENSE_21:
                evnt = UpgradePlayerStatsEvent.Create(GlobalTargets.OnlyServer);
                evnt.DeltaHP = 0.0f;
                evnt.DeltaBaseDamage = 0;
                evnt.DeltaAttackSpeed = 0.35f;
                evnt.DeltaMovementSpeed = 0.0f;
                evnt.PlayerEntity = PlayerData.myPlayer;
                evnt.Send();
                break;
            default:
                break;
        }
    }

    public bool isUnlockable()
    {
        if (ParentItem == null)
            return true;
        return ParentItem.GetIsUnlocked();
    }

    public bool GetIsUnlocked()
    {
        return is_unlocked;
    }

    public void UnlockItem()
    {
        if (!isUnlockable() || is_unlocked)
            return;


        MarkUnlocked("You");
        OnUnlock?.Invoke();

    }

    public void MarkUnlocked(string buyer_name)
    {
        if (is_unlocked)
            return;

        is_unlocked = true;

        NotificationsManager.instance.AddShopItemUI(buyer_name, ItemIcon.sprite);

        foreach (Image childlink in ChildLinks)
        {
            childlink.color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        }
        IconCanvasGroup.alpha = 0.7f;

    }

    private void SendTeamUpgradeEvent(int team, int item_id)
    {
        TeamUpgradeEvent evnt = TeamUpgradeEvent.Create(GlobalTargets.Everyone);
        evnt.TeamID = team;
        evnt.UpgradeID = item_id;
        evnt.Buyer = PlayerData.myPlayer;
        evnt.Send();
    }

}



public enum ITEM_TYPE
{
    UNSET = 0,
    LANTERN_UPGRADE = 1,
    PLAYER_SPEED_0 = 10,
    PLAYER_SPEED_1 = 11,
    PLAYER_SPEED_2 = 12,
    WEAPON_UPGRADE_0 = 20,
    WEAPON_UPGRADE_1 = 21,
    WEAPON_UPGRADE_21 = 22,
    WEAPON_UPGRADE_22 = 23,
    CRAFTING_UPGRADE_0 = 31,
    CRAFTING_UPGRADE_10 = 32,
    CRAFTING_UPGRADE_11 = 33,
    PLAYER_HP_0 = 40,
    PLAYER_HP_1 = 41,
    PLAYER_HP_2 = 42,
    PLAYER_OFFENSE_0 = 50,
    PLAYER_OFFENSE_1 = 51,
    PLAYER_OFFENSE_20 = 52,
    PLAYER_OFFENSE_21 = 53,

    NUM_TOTAL
}