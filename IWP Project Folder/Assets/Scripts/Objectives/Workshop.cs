using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Workshop : MonoBehaviour
{
    CanvasGroup WorkshopVisibilityIcon;

    private bool in_range = false;

    void Start()
    {
        StartCoroutine(InitWorkshop());
    }

    void Update()
    {
        if (in_range && !WorkshopManager.IsWorkshopOpen)
        {
            if (Input.GetKeyDown(GameSettings.TOGGLE_WORKSHOP_KEY))
            {
                WorkshopManager.instance.ToggleWorkshop(true);
            }
        }
    }

    IEnumerator InitWorkshop()
    {
        while (UIManager.instance == null)
            yield return null;

        WorkshopVisibilityIcon = UIManager.instance.WorkshopVisibilityIcon;
        WorkshopVisibilityIcon.alpha = 0.2f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == PlayerData.myPlayer.gameObject && !other.isTrigger)
        {
            WorkshopVisibilityIcon.alpha = 0.7f;
            in_range = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == PlayerData.myPlayer.gameObject && !other.isTrigger)
        {
            WorkshopVisibilityIcon.alpha = 0.2f;
            in_range = false;
        }
    }
}
