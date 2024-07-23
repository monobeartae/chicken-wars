using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamCraftNotifUI : NotifUI
{
    public Text playerName;
    public Image weaponIcon;

    public void Init(Sprite item_sprite, string player_name)
    {
        playerName.text = player_name;
        weaponIcon.sprite = item_sprite;
    }
}
