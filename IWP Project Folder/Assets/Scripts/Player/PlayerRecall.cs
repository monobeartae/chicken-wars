using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class PlayerRecall : EntityBehaviour<IPlayerState>
{
    private CanvasGroup RecallUI;
    private Image RecallFill;
    public ParticleSystem RecallEffect;
    public AudioSource RecallSFX;


    private float RECALL_TIMER = 3.0f; // ori 6
    private PlayerController playerController = null;
    private PlayerMotor playerMotor = null;


    public override void Attached()
    {

        state.OnStartRecall += StartPlayerRecall;

        StartCoroutine(PlayerRecallInits());
    }

    IEnumerator PlayerRecallInits()
    {

        playerMotor = gameObject.GetComponent<PlayerMotor>();
        playerController = gameObject.GetComponent<PlayerController>();

        while (UIManager.instance == null)
            yield return null;
        RecallUI = UIManager.instance.PlayerRecallUI;
        RecallFill = UIManager.instance.PlayerRecallFill;
    }
 
    void Update()
    {
        if (!entity.HasControl)
            return;

        if (Input.GetKeyDown(GameSettings.RECALL_KEY))
        {
            PlayerRecallEvent evnt = PlayerRecallEvent.Create(GlobalTargets.OnlyServer);
            evnt.PlayerEntity = this.entity;
            evnt.Send();
        }
    }

    void StartPlayerRecall()
    {
        StartCoroutine(StartRecall());
    }

    public void EndPlayerRecall(bool complete)
    {
        RecallEffect.Pause();
        RecallEffect.gameObject.SetActive(false);
        if (entity.HasControl)
        {
            RecallUI.alpha = 0.3f;
            RecallFill.fillAmount = 0.0f;
        }
        if (complete && BoltNetwork.IsServer)
            playerController.SetToSpawn();

        if (!complete)
            RecallSFX.Stop();

    }

    IEnumerator StartRecall()
    {
        RecallEffect.gameObject.SetActive(true);
        RecallEffect.Play();
        RecallSFX.Play();

        if (entity.HasControl)
        {
            RecallUI.alpha = 0.7f;
        }


        if (!entity.HasControl)
            yield break;

        float timer = RECALL_TIMER;
        while (timer > 0.0f && !playerMotor.playerHasMovement)
        {
            timer -= Time.deltaTime;
            RecallFill.fillAmount = (RECALL_TIMER - timer) / RECALL_TIMER;
            yield return null;
        }

        bool complete = false;
        if (timer <= 0.0f)
            complete = true;

        PlayerEndRecallEvent evnt = PlayerEndRecallEvent.Create(GlobalTargets.Everyone);
        evnt.PlayerEntity = this.entity;
        evnt.IsComplete = complete;
        evnt.Send();

    }
}

