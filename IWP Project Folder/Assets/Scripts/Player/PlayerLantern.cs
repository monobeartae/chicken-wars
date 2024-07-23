using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class PlayerLantern : EntityBehaviour<IPlayerState>
{
    public Light PlayerBaseLight;
    public Light PlayerLanternLight;

    public AudioSource OnLanternSFX;
    public AudioSource OffLanternSFX;

    private LightFlicker LanternLightFlicker;

    private CanvasGroup LanternUI;
    private Image LanternFuelOverlay;

    private int LANTERN_LEVEL = 0;
    private LANTERN_STATE lanternState = LANTERN_STATE.STRONG;

    private float MAX_INTENSITY = 1.0f;
    private float CURRENT_INTENSITY = 1.0f;
    private float MIN_INTENSITY = 0.3f;
    private float MAX_RANGE = 90.0f;
    private float MIN_RANGE = 80.0f;

    private bool is_lit = false;
    private float fuel_use_rate = 1.5f;
    private float fuel_amount = 100.0f;
    private float MAX_FUEL = 100.0f;

    private Color fuelLowColor = new Color32(255, 60, 60, 255);

    public override void Attached()
    {
        LanternLightFlicker = PlayerLanternLight.GetComponent<LightFlicker>();
        LanternLightFlicker.enabled = false;
        this.enabled = false;
    }

    public void Init()
    {
        StartCoroutine(PlayerLanternInits());
    }

    IEnumerator PlayerLanternInits()
    {
        if (entity.HasControl)
        {
            PlayerBaseLight.gameObject.SetActive(true);
            PlayerLanternLight.gameObject.SetActive(false);
            SetLanternStats();
            PlayerLanternLight.spotAngle = MAX_RANGE;
            PlayerLanternLight.intensity = MAX_INTENSITY;
        }

        while (UIManager.instance == null)
            yield return null;

        if (entity.HasControl)
        {
            UIManager.instance.LanternToggleButton.onClick.AddListener(ToggleLanternLight);

            LanternUI = UIManager.instance.LanternUI;
            LanternFuelOverlay = LanternUI.transform.Find("Overlay").GetComponent<Image>();

            LanternUI.alpha = 0.2f;
            LanternFuelOverlay.fillAmount = 1.0f;
        }
    }

    public void ToggleLanternLight()
    {
        if (is_lit)
        {
            ToggleLanternLight(false);
        }
        else if (fuel_amount > 0)
        {
            ToggleLanternLight(true);
        }
    }

    void ToggleLanternLight(bool on)
    {
        if (!on)
        {
            // Turn Off
            OffLanternSFX.Play();
            is_lit = false;
            PlayerBaseLight.gameObject.SetActive(true);
            PlayerLanternLight.gameObject.SetActive(false);
            LanternUI.alpha = 0.2f;
        }
        else if (fuel_amount > 0)
        {
            // Turn On
            OnLanternSFX.Play();
            is_lit = true;
            PlayerBaseLight.gameObject.SetActive(false);
            PlayerLanternLight.gameObject.SetActive(true);
            LanternUI.alpha = 0.7f;
        }
    }
     
    void Update()
    {

        if (!entity.HasControl)
            return;

        if (Input.GetKeyDown(GameSettings.TOGGLE_LANTERN_KEY))
            ToggleLanternLight();

        if (is_lit)
        {
            fuel_amount -= fuel_use_rate * Time.deltaTime;
            float perc = fuel_amount / MAX_FUEL;
            LanternFuelOverlay.fillAmount = perc;

            switch (lanternState)
            {
                case LANTERN_STATE.STRONG:
                    if (perc < 0.5f)
                    {
                        lanternState = LANTERN_STATE.FADING;
                    }
                    break;
                case LANTERN_STATE.FADING:
                    CURRENT_INTENSITY = MIN_INTENSITY + 2 * perc * (MAX_INTENSITY - MIN_INTENSITY);
                    PlayerLanternLight.intensity = CURRENT_INTENSITY;
                    PlayerLanternLight.spotAngle = MIN_RANGE + 2 * perc * (MAX_RANGE - MIN_RANGE);
                    if (perc < 0.2f)
                    {
                        lanternState = LANTERN_STATE.LOW;
                        LanternFuelOverlay.color = fuelLowColor;
                        LanternLightFlicker.enabled = true;
                    }
                    else if (perc > 0.5f)
                    {
                        lanternState = LANTERN_STATE.STRONG;
                        SetLanternLightToMax();
                    }
                    break;
                case LANTERN_STATE.LOW:
                    CURRENT_INTENSITY = MIN_INTENSITY + 2 * perc * (MAX_INTENSITY - MIN_INTENSITY);
                    LanternLightFlicker.UpdateIntensityLimit(CURRENT_INTENSITY);
                    PlayerLanternLight.spotAngle = MIN_RANGE + 2 * perc * (MAX_RANGE - MIN_RANGE);
                    if (perc > 0.2f)
                    {
                        lanternState = LANTERN_STATE.FADING;
                        LanternFuelOverlay.color = Color.white;
                        LanternLightFlicker.enabled = false;
                    }
                    else if (perc <= 0)
                    {
                        fuel_amount = 0;
                        ToggleLanternLight(false);
                    }
                    break;
                default:
                    break;
            }
        }
    }
    
    void SetLanternLightToMax()
    {
        PlayerLanternLight.spotAngle = MAX_RANGE;
        PlayerLanternLight.intensity = MAX_INTENSITY;
    }

    void SetLanternStats()
    {
        float fuel_perc = fuel_amount / MAX_FUEL;

        switch (LANTERN_LEVEL)
        {
            case 0: // Starting Lantern Stats
                MAX_INTENSITY = 1.0f;
                MAX_RANGE = 90.0f;
                MIN_RANGE = 80.0f;
                break;
            case 1:
                MAX_INTENSITY = 1.7f;
                MAX_RANGE = 110.0f;
                MIN_RANGE = 100.0f;
                MAX_FUEL += 10;
                break;
            case 2:
                MAX_INTENSITY = 2.2f;
                MAX_RANGE = 130.0f;
                MIN_RANGE = 115.0f;
                MAX_FUEL += 10;
                break;
            case 3:
                MAX_INTENSITY = 3.0f;
                MAX_RANGE = 140.0f;
                MIN_RANGE = 125.0f;
                MAX_FUEL += 10;
                break;
        }

        fuel_amount = fuel_perc * MAX_FUEL;

        float new_intensity = MIN_INTENSITY + fuel_perc * (MAX_INTENSITY - MIN_INTENSITY);
        PlayerLanternLight.intensity = new_intensity;
        PlayerLanternLight.spotAngle = MIN_RANGE + fuel_perc * (MAX_RANGE - MIN_RANGE);


    }

    public void UpgradeLanternLight()
    {
        LANTERN_LEVEL++;
        SetLanternStats();
    }

    public float GetLanternPerc()
    {
        return (fuel_amount / MAX_FUEL) * 100.0f;
    }
    public void RefillLantern(float new_perc)
    {
        fuel_amount = (new_perc / 100.0f) * MAX_FUEL;
    }

    public void DisablePlayerLantern()
    {
        this.enabled = false;
        ToggleLanternLight(false);
    }
    public void EnablePlayerLantern()
    {
        this.enabled = true;
        ToggleLanternLight(true);
    }
}

enum LANTERN_STATE
{
    STRONG,
    FADING,
    LOW,

    NUM_TOTAL
}