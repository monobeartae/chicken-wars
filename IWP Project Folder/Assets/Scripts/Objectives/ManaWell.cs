using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Bolt;

public class ManaWell : MonoBehaviour
{
    private PlayerLantern playerLantern;
    private float recharge_rate = 5.0f; // ori was 3
    private float fuel_perc = 0.0f; //temp var

    private GameObject refillUI;
    private Image refill_image;
    private Text refill_text;

    WELL_STATE state = WELL_STATE.DEFAULT;

    private static PlayerController playerController = null;
    private Guid popupID;

    void Start()
    {
        StartCoroutine(ManaWellInits());
    }
    IEnumerator ManaWellInits()
    {
        while (UIManager.instance == null)
            yield return null;

        refillUI = UIManager.instance.LanternRefillPanel;
        refill_image = UIManager.instance.LanternRefillBar;
        refill_text = UIManager.instance.LanternRefillText;
        UIManager.instance.LanternStopRefillButton.onClick.AddListener(StopCharge);

        while (PlayerData.myPlayer == null)
            yield return null;

        playerLantern = PlayerData.myPlayer.gameObject.GetComponent<PlayerLantern>();
        playerController = PlayerData.myPlayer.gameObject.GetComponent<PlayerController>();
    }
    void Update()
    {
        switch (state)
        {
            case WELL_STATE.CHARGING:
                fuel_perc += recharge_rate * Time.deltaTime;
                refill_image.fillAmount = fuel_perc / 100.0f;
                refill_text.text = "MANA: " + ((int)fuel_perc).ToString() + "%";
                if (!Input.GetKey(GameSettings.INTERACT_KEY))
                {
                    StopCharge();
                }
                else if (fuel_perc >= 100.0f)
                {
                    fuel_perc = 100.0f;
                    StopCharge();
                }
                break;
            default:
                break;
        }
    }
    void ChargeLantern()
    {
        state = WELL_STATE.CHARGING;
        refillUI.SetActive(true);

        playerController.LOCK_PLAYER_MOVEMENT = true;

        playerLantern.DisablePlayerLantern();
        fuel_perc = playerLantern.GetLanternPerc();
    }
    void StopCharge()
    {
        if (state != WELL_STATE.CHARGING)
            return;
        state = WELL_STATE.DEFAULT;
        refillUI.SetActive(false);

        playerController.LOCK_PLAYER_MOVEMENT = false;

        playerLantern.EnablePlayerLantern();
        playerLantern.RefillLantern(fuel_perc);
    }
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !other.isTrigger
            && other.gameObject.GetComponent<BoltEntity>() == PlayerData.myPlayer)
        {

            if (Input.GetKeyDown(GameSettings.INTERACT_KEY) && state == WELL_STATE.DEFAULT)
            {
                ChargeLantern();
            }
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !other.isTrigger
            && other.gameObject.GetComponent<BoltEntity>() == PlayerData.myPlayer)
        {
            popupID = UIManager.instance.SetInteractPopUp("Refill Mana", "E");
        }

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !other.isTrigger
            && other.gameObject.GetComponent<BoltEntity>() == PlayerData.myPlayer)
        {
            UIManager.instance.DisableInteractPopUp(popupID);
        }

    }

    enum WELL_STATE
    {
        DEFAULT,
        CHARGING,

        NUM_TOTAL
    }
}
