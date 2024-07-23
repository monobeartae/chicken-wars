using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager instance = null;
    #endregion

    public Camera UICamera;

    public GameObject UI_ESCMenu;
    public GameObject UI_LoadingScreen;
    public GameObject UI_RespawnScreen;
    public GameObject UI_ShopPanel;
    public GameObject UI_WorkshopPanel;
    public GameObject UI_VictoryScreen;
    public GameObject UI_DefeatScreen;
    public GameObject UI_Minimap;

    public CanvasGroup ShopVisibilityIcon;
    public CanvasGroup WorkshopVisibilityIcon;

    public Text PlayerCoinsAmount;
    public Text TeamOwnedMetalBars, TeamOwnedCrystalOres;

    public CanvasGroup[] weaponSlots = new CanvasGroup[2];
    public Image[] WeaponSlotIcons = new Image[2];
    public Image[] WeaponDurabilityFillBars = new Image[2];
    public Image[] WeaponAttackIntervalOverlay = new Image[2];

    public CanvasGroup LanternUI;
    public Button LanternToggleButton;
    public GameObject LanternRefillPanel;
    public Button LanternStopRefillButton;
    public Image LanternRefillBar;
    public Text LanternRefillText;

    public CanvasGroup PlayerRecallUI;
    public Image PlayerRecallFill;

    public GameObject[] PopUps;
    private List<PopUp> activePopUplist = new List<PopUp>();

    public Text TEST_TEXT;

    private Guid activeInteractGuid;
    public static Vector3 offset;

    private static int UILayer;

    void Start()
    {
        instance = this;
        offset = UICamera.transform.localPosition;
        UILayer = LayerMask.NameToLayer("ScreenUI");
        TeamInventory.OnOwnedMaterialsChanged += UpdateTeamMaterialsUI;
        PlayerData.OnPlayerCoinsChanged += UpdatePlayerCoinsUI;
        UpdatePlayerCoinsUI();
    }


    void Update()
    {
        foreach (PopUp popup in activePopUplist)
        {
            popup.UpdatePopUp();
            if (popup.timer <= 0.0f)
            {
                Destroy(popup.popupGO);
            }
           
        }

        activePopUplist.RemoveAll(s => s.popupGO == null);
    }

    public Guid SetInteractPopUp(string display_text, string key = "Ë")
    {
        GameObject popup = PopUps[(int)POPUP_TYPE.INTERACT];
        popup.SetActive(true);
        popup.transform.Find("Text").GetComponent<Text>().text = display_text;

        activeInteractGuid = System.Guid.NewGuid();
        return activeInteractGuid;
    }

    public void DisableInteractPopUp(Guid id)
    {
        if (activeInteractGuid == id)
            PopUps[(int)POPUP_TYPE.INTERACT].SetActive(false);
    }

    public void AddDamageTakenText(float dmg, Transform playerTransform)
    {
        //  Vector3 screenPos = Camera.main.WorldToScreenPoint(playerPos);
        //  screenPos = new Vector3(screenPos.x, screenPos.y, 0.0f);
        GameObject popup_go = Instantiate(PopUps[(int)POPUP_TYPE.DAMAGE], Vector3.zero, Quaternion.identity);
        popup_go.transform.SetParent(playerTransform);

        Text dmg_text = popup_go.GetComponent<Text>();
        dmg_text.text = dmg.ToString();
        dmg_text.color = Color.red;
        PopUp popup = new PopUp();
        popup.Init(popup_go, 0.8f);
        activePopUplist.Add(popup);
    }

    public void AddDamageDealtText(float dmg, Transform enemyTransform)
    {
      //  Vector3 screenPos = Camera.main.WorldToScreenPoint(enemyPos);
      //  screenPos = new Vector3(screenPos.x, screenPos.y, 0.0f);
        GameObject popup_go = Instantiate(PopUps[(int)POPUP_TYPE.DAMAGE], Vector3.zero, Quaternion.identity);
        popup_go.transform.SetParent(enemyTransform);
       

        Text dmg_text = popup_go.GetComponent<Text>();
        dmg_text.text = dmg.ToString();
        dmg_text.color = Color.white;
        PopUp popup = new PopUp();
        popup.Init(popup_go, 0.8f);
        activePopUplist.Add(popup);
    }

    public void SetLoadingScreen(bool active)
    {
        UI_LoadingScreen.SetActive(active);
    }
    public void SetRespawnScreen(bool active)
    {
        UI_RespawnScreen.SetActive(active);
    }
    public void UpdateRespawnTimer(int time)
    {
        UI_RespawnScreen.transform.Find("RespawnTimer").GetComponent<Text>().text = time.ToString();
    }
    public void ToggleESCMenu()
    {
        UI_ESCMenu.SetActive(!UI_ESCMenu.activeSelf);
    }
    public void ShowVictoryScreen()
    {
        UI_VictoryScreen.SetActive(true);
    }
    public void ShowDefeatScreen()
    {
        UI_DefeatScreen.SetActive(true);
    }

    void UpdatePlayerCoinsUI()
    {
        PlayerCoinsAmount.text = PlayerData.PlayerCoins.ToString();
    }

    void UpdateTeamMaterialsUI()
    {
        TeamOwnedCrystalOres.text = TeamInventory.TeamCrystalOres.ToString();
        TeamOwnedMetalBars.text = TeamInventory.TeamMetalBars.ToString();
    }

    public void ToggleShopUI()
    {
        UI_ShopPanel.SetActive(!UI_ShopPanel.activeSelf);

        ToggleShopUI(UI_ShopPanel.activeSelf);
    }

    public void ToggleShopUI(bool open)
    {
        UI_ShopPanel.SetActive(open);

        weaponSlots[0].gameObject.SetActive(!open);
        weaponSlots[1].gameObject.SetActive(!open);

        ShopManager.IsShopOpen = open;
    }

    public void ToggleWorkshopUI(bool active)
    {
        UI_WorkshopPanel.SetActive(active);

        bool shopOpen = UI_WorkshopPanel.activeSelf;

        weaponSlots[0].gameObject.SetActive(!active);
        weaponSlots[1].gameObject.SetActive(!active);

        WorkshopManager.IsWorkshopOpen = active;
    }

    public void SetWeaponAttackInterval(int index, float perc_left)
    {
        WeaponAttackIntervalOverlay[index].fillAmount = perc_left;
    }

    public void DisableAllUI()
    {
        LanternUI.gameObject.SetActive(false);
        weaponSlots[0].gameObject.SetActive(false);
        weaponSlots[1].gameObject.SetActive(false);
        UI_ShopPanel.SetActive(false);
        ShopVisibilityIcon.gameObject.SetActive(false);
        UI_Minimap.SetActive(false);
    }

    public void SetTestText(string what)
    {
        TEST_TEXT.gameObject.SetActive(true);
        TEST_TEXT.text = what;
    }

    // Returns 'true' if we touched or hovering on Unity UI element.
    public static bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    // Returns 'true' if we touched or hovering on Unity UI element.
    private static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }

    // Gets all event system raycast results of current mouse or touch position.
    private static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}


enum POPUP_TYPE
{
    INTERACT = 0,
    DAMAGE = 1,

    NUM_TOTAL
}

class PopUp
{
    public GameObject popupGO;

    public float timer;
    private float fly_speed;
    
    public void Init(GameObject go, float max_timer)
    {
        popupGO = go;
        timer = max_timer;
        fly_speed = 0.5f;

        float radial_offset = 0.15f;
        float player_y_offset = 0.24f;
        Vector3 offset = new Vector3(UnityEngine.Random.Range(-1 * radial_offset, radial_offset), player_y_offset, UnityEngine.Random.Range(-1 * radial_offset, radial_offset));
        offset += UIManager.offset;
        popupGO.transform.localPosition = offset;

        popupGO.AddComponent<WorldSpaceUI>();

    }

    public void UpdatePopUp()
    {
        if (popupGO == null)
            return;

        popupGO.transform.position = new Vector3(
            popupGO.transform.position.x,
            popupGO.transform.position.y + (fly_speed * Time.deltaTime),
            popupGO.transform.position.z);

        timer -= Time.deltaTime;
      
    }
}