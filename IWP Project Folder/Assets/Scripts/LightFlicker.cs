using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    enum LIGHT_STATE
    {
        ON,
        TURNING_OFF,
        OFF,
        TURNING_ON,

        NUM_TOTAL
    }


    public float MAX_FADE_TIMING = 0.5f;
    public float MIN_FADE_TIMING = 0.0f;
    public float MAX_ON_TIMING = 2.0f;
    public float MAX_OFF_TIMING = 0.5f;
    public float MIN_ON_TIMING = 1.0f;
    public float MIN_OFF_TIMING = 0.0f;

    public float LightOnIntensityPercentage = 0.5f;
    public float LightOffIntensityPercentage = 0.75f;

    Light light;
    float MAX_INTENSITY;

    LIGHT_STATE state = LIGHT_STATE.ON;

    float change_speed = 0.0f;
    float state_timer = 0.0f;
    float light_intensity = 0.0f;



    void Start()
    {
        light = GetComponent<Light>();
        MAX_INTENSITY = 1.0f;
    }


    public void UpdateIntensityLimit(float intensity)
    {
        MAX_INTENSITY = intensity;
    }

    void Update()
    {

        if (state_timer > 0)
            state_timer -= Time.deltaTime;


        switch (state)
        {
            case LIGHT_STATE.ON:
                if (state_timer <= 0)
                {
                    state = LIGHT_STATE.TURNING_OFF;
                    state_timer = Random.Range(MIN_FADE_TIMING, MAX_FADE_TIMING);
                    light_intensity = Random.Range(0.0f, MAX_INTENSITY * LightOffIntensityPercentage);
                    change_speed = (light.intensity - light_intensity) / state_timer;
                }
                break;
            case LIGHT_STATE.TURNING_OFF:
                light.intensity += change_speed * Time.deltaTime;
                if (state_timer <= 0)
                {
                    state = LIGHT_STATE.OFF;
                    light.intensity = light_intensity;
                    state_timer = Random.Range(MIN_OFF_TIMING, MAX_OFF_TIMING);
                }
                break;
            case LIGHT_STATE.OFF:
                if (state_timer <= 0)
                {
                    state = LIGHT_STATE.TURNING_ON;
                    state_timer = Random.Range(MIN_FADE_TIMING, MAX_FADE_TIMING);
                    light_intensity = Random.Range(MAX_INTENSITY * LightOnIntensityPercentage, MAX_INTENSITY);
                    change_speed = (light.intensity - light_intensity) / state_timer;
                }
                break;
            case LIGHT_STATE.TURNING_ON:
                light.intensity += change_speed * Time.deltaTime;
                if (state_timer <= 0)
                {
                    state = LIGHT_STATE.ON;
                    light.intensity = light_intensity;
                    state_timer = Random.Range(MIN_ON_TIMING, MAX_ON_TIMING);
                }
                break;
        }

        if (light.intensity > MAX_INTENSITY)
            light.intensity = MAX_INTENSITY;
    }



}
