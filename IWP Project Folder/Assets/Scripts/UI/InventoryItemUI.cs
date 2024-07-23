using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public Sprite EmptyIcon;

    public Image icon;
    public Image durabilityFill;

    public Button button;

    private Weapon LinkedWeapon = null;
    private event Action<InventoryItemUI> SetSelectedItem;

    private Color32 emptyColor = new Color32(255, 255, 255, 150);

    public void InitEmpty(Action<InventoryItemUI> action)
    {
        icon.sprite = EmptyIcon;
        icon.color = emptyColor;
        durabilityFill.gameObject.SetActive(false);

        button.onClick.AddListener(OnButtonClicked);

        SetSelectedItem = action;
    }

    void OnButtonClicked()
    {
        SetSelectedItem?.Invoke(this);
    }

    public void Init(Weapon weapon, Action<InventoryItemUI> action)
    {
        icon.sprite = weapon.weaponIconSprite;
        icon.color = Color.white;
        durabilityFill.gameObject.SetActive(true);
        durabilityFill.fillAmount = weapon.GetDurability() / weapon.GetMaxDurability();
        if (durabilityFill.fillAmount < 0.2f)
            durabilityFill.color = new Color32(255, 150, 150, 255);
        else
            durabilityFill.color = Color.white;

        button.onClick.AddListener(OnButtonClicked);

        LinkedWeapon = weapon;
        weapon.gameObject.SetActive(false);

        SetSelectedItem = action;
    }

    public Weapon GetWeapon()
    {
        return LinkedWeapon;
    }
}
