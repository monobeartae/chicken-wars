using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamInventoryNotifUI : NotifUI
{
    public Text playerName;
    public Image weaponIcon;
    public Image tradeIcon;
    public Sprite inIcon, outIcon;

    public void Init(Sprite item_sprite, string player_name, bool place_in)
    {
        playerName.text = player_name;
        weaponIcon.sprite = item_sprite;
        if (place_in)
            tradeIcon.sprite = inIcon;
        else
            tradeIcon.sprite = outIcon;
    }

}
