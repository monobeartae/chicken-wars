using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationsManager : MonoBehaviour
{
    #region Singleton
    public static NotificationsManager instance = null;
    #endregion Singleton

    public Transform NotificationsUIRect;
    public GameObject CurrencyEarnedNotif, TeamShopNotif, TeamCraftNotif, TeamInventoryNotif;
    public Sprite CoinsSprite, MetalBarsSprite, CrystalOresSprite;

    public Animator notifAnimator;

    public GameObject PlayerKillPanel;
    public Text DeadPlayerName, KillerName;
    public Image KillIcon;
    public Sprite DeathSprite, KillSprite;
    private CanvasGroup PlayerKillCanvasGroup;

    public AudioSource PlayerKillSFX;

    private List<NotifUI> notifUIList = new List<NotifUI>();
    private Coroutine PlayerKillNotifCor = null;


    void Start()
    {
        instance = this;

        PlayerKillCanvasGroup = PlayerKillPanel.GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (notifUIList.Count > 0)
        {
            notifUIList[0].UpdateNotif();
            if (!notifUIList[0].active)
            {
                NotifUI to_remove = notifUIList[0];
                notifUIList.Remove(to_remove);
                Destroy(to_remove.gameObject);
            }
        }
    }

    private void ListUpdated()
    {
        notifAnimator.SetTrigger("NewNotif");

        if (notifUIList.Count > 5)
        {
            NotifUI to_remove = notifUIList[0];
            notifUIList.Remove(to_remove);
            Destroy(to_remove.gameObject);
        }

    }


    public void AddCurrencyEarnedUI(CURRENCY_TYPE curr_type, int amt)
    {
        Sprite temp = null;
        switch (curr_type)
        {
            case CURRENCY_TYPE.COINS:
                temp = CoinsSprite;
                break;
            case CURRENCY_TYPE.METAL_BARS:
                temp = MetalBarsSprite;
                break;
            case CURRENCY_TYPE.CRYSTAL_ORES:
                temp = CrystalOresSprite;
                break;

        }
        GameObject notif_go = Instantiate(CurrencyEarnedNotif, NotificationsUIRect);
        CurrencyNotifUI notif = notif_go.GetComponent<CurrencyNotifUI>();
        notif.Init(temp, amt);
        notifUIList.Add(notif);
        ListUpdated();
    }

    public void AddTeamCraftUI(string player_name, Sprite weapon_icon)
    {
        GameObject notif_go = Instantiate(TeamCraftNotif, NotificationsUIRect);
        TeamCraftNotifUI notif = notif_go.GetComponent<TeamCraftNotifUI>();
        notif.Init(weapon_icon, player_name);
        notifUIList.Add(notif);
        ListUpdated();
    }

    public void AddTakeFromInventoryUI(string player_name, Sprite weapon_icon)
    {
        GameObject notif_go = Instantiate(TeamInventoryNotif, NotificationsUIRect);
        TeamInventoryNotifUI notif = notif_go.GetComponent<TeamInventoryNotifUI>();
        notif.Init(weapon_icon, player_name, false);
        notifUIList.Add(notif);
        ListUpdated();
    }

    public void AddPlaceIntoInventoryUI(string player_name, Sprite weapon_icon)
    {
        GameObject notif_go = Instantiate(TeamInventoryNotif, NotificationsUIRect);
        TeamInventoryNotifUI notif = notif_go.GetComponent<TeamInventoryNotifUI>();
        notif.Init(weapon_icon, player_name, true);
        notifUIList.Add(notif);
        ListUpdated();
    }

    public void AddShopItemUI(string player_name, Sprite shop_icon)
    {
        GameObject notif_go = Instantiate(TeamShopNotif, NotificationsUIRect);
        ShopItemNotifUI notif = notif_go.GetComponent<ShopItemNotifUI>();
        notif.Init(shop_icon, player_name);
        notifUIList.Add(notif);
        ListUpdated();
    }

    public void AddKillNotif(bool isAllyKill, string killer, string dead)
    {
        // Set KillPanel Details
        DeadPlayerName.text = dead;
        KillerName.text = killer;
        if (isAllyKill)
            KillIcon.sprite = KillSprite;
        else
            KillIcon.sprite = DeathSprite;

        PlayerKillSFX.Play();

        // Control KillPanel
        bool skip = false;
        if (PlayerKillNotifCor != null)
        {
            StopCoroutine(PlayerKillNotifCor);
            skip = true;
        }

        PlayerKillNotifCor = StartCoroutine(PlayerKillNotif(skip));

    }

    IEnumerator PlayerKillNotif(bool skip_fadein)
    {
        // SHOW PANEL
        PlayerKillPanel.gameObject.SetActive(true);

        // FADE IN
        float fade_speed = 0.5f;
        float starting_alpha = 0.0f;
        if (skip_fadein)
            starting_alpha = PlayerKillCanvasGroup.alpha;

        PlayerKillCanvasGroup.alpha = starting_alpha;

        while (PlayerKillCanvasGroup.alpha < 1.0f)
        {
            PlayerKillCanvasGroup.alpha += fade_speed * Time.deltaTime;
            yield return null;
        }
        PlayerKillCanvasGroup.alpha = 1.0f;

        // Show and Wait
        yield return new WaitForSeconds(3.0f);

        // FADE OUT
        while (PlayerKillCanvasGroup.alpha > 0.0f)
        {
            PlayerKillCanvasGroup.alpha -= fade_speed * Time.deltaTime;
            yield return null;
        }
        PlayerKillCanvasGroup.alpha = 0.0f;

        // HIDE PANEL
        PlayerKillPanel.gameObject.SetActive(false);


    }
}

public class NotifUI : MonoBehaviour
{
    public bool active = true;

    private float timer = 3.0f;
    private float anim_timer = 1.0f;
    private float max_anim_timer = 1.0f;
    private bool isDisappearing = false;
  
    public void UpdateNotif()
    {
        if (!isDisappearing)
        {
            timer -= Time.deltaTime;
            
            if (timer <= 0)
            {
                isDisappearing = true;
                StartCoroutine(FadeAnim());
            }
        }
    }

    IEnumerator FadeAnim()
    {
        anim_timer = max_anim_timer;

        while (anim_timer > 0.0f)
        {
            anim_timer -= Time.deltaTime;
            gameObject.GetComponent<CanvasGroup>().alpha = anim_timer / max_anim_timer;
            yield return null;
        }
        active = false;
    }
}

public enum CURRENCY_TYPE
{
    COINS,
    METAL_BARS,
    CRYSTAL_ORES,

    NUM_TOTAL
}
