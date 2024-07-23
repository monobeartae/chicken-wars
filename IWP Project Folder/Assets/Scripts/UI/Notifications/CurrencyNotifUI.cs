using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyNotifUI : NotifUI
{
    public Image currencyIcon;
    public Text currencyAmt;


    public void Init(Sprite curr_sprite, float amt)
    {
        currencyIcon.sprite = curr_sprite;
        string amt_s = "";
        if (amt >= 0)
            amt_s += "+";
        amt_s += amt.ToString();
        currencyAmt.text = amt_s;
    }
}
