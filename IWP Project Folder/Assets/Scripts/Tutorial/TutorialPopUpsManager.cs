using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPopUpsManager : MonoBehaviour
{
    public GameObject PopUpsList;

    private List<TutorialPopUp> popupList = new List<TutorialPopUp>();
    private List<TutorialPopUp> active_popupList = new List<TutorialPopUp>();
    TutorialPopUp current_popup = null;

    void Start()
    {
        foreach (Transform child in PopUpsList.transform)
        {
            // Add PopUp to list
            TutorialPopUp popup = child.gameObject.GetComponent<TutorialPopUp>();
            popupList.Add(popup);
        }
    }

    void Update()
    {
        if (current_popup == null)
        {
            if (active_popupList.Count > 0)
            {
                // Activate Next PopUp
                current_popup = active_popupList[0];
                current_popup.gameObject.SetActive(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            current_popup.gameObject.SetActive(false);
            active_popupList.Remove(current_popup);
            if (active_popupList.Count > 0)
            {
                // Activate Next PopUp
                current_popup = active_popupList[0];
                current_popup.gameObject.SetActive(true);
            }
            else
            {
                current_popup = null;
            }
        }
    }

    public void QueuePopUp(PopUpID id)
    {
        TutorialPopUp popup = Find(id);
        if (popup == null)
            return;
        active_popupList.Add(popup);
    }

    private TutorialPopUp Find(PopUpID id)
    {
        foreach (TutorialPopUp popup in popupList)
        {
            if (popup.ID == id)
                return popup;
        }
        return null;
    }
}
