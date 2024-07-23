using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{

    CanvasGroup ShopVisibilityIcon;

    private bool in_range = false;

    void Start()
    {
        StartCoroutine(InitShop());
    }

    void Update()
    {
        if (in_range)
        {
            if (Input.GetKeyDown(GameSettings.TOGGLE_SHOP_KEY))
            {
                ShopManager.instance.ToggleShop();
            }
        }
    }

    IEnumerator InitShop()
    {
        while (UIManager.instance == null)
            yield return null;

        ShopVisibilityIcon = UIManager.instance.ShopVisibilityIcon;
        ShopVisibilityIcon.alpha = 0.2f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == PlayerData.myPlayer.gameObject && !other.isTrigger)
        {
            ShopVisibilityIcon.alpha = 0.7f;
            in_range = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (in_range)
            return;

        if (other.gameObject == PlayerData.myPlayer.gameObject && !other.isTrigger)
        {
            ShopVisibilityIcon.alpha = 0.7f;
            in_range = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == PlayerData.myPlayer.gameObject && !other.isTrigger)
        {
            ShopVisibilityIcon.alpha = 0.2f;
            in_range = false;
        }
    }
}
