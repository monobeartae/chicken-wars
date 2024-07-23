using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemNotifUI : NotifUI
{
    public Text playerName;
    public Image itemIcon;

    public void Init(Sprite item_sprite, string player_name)
    {
        playerName.text = player_name;
        itemIcon.sprite = item_sprite;
    }
}
