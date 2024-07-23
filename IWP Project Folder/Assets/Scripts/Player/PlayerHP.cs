using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class PlayerHP : EntityBehaviour<IPlayerState>
{
    public GameObject HPBar;
    public Image HPFill;
    // Minimap UI
    public Image MinimapIcon;

    private GameObject PlayerCanvas = null;
    private PlayerController playerController = null;
    private PlayerMotor playerMotor = null;
    private PlayerLantern playerLantern = null;
    private PlayerData playerData = null;

    private float RESPAWN_TIME = 5.0f;
    private float respawn_timer = 0.0f;

    public override void Attached()
    {
        PlayerCanvas = transform.Find("Canvas").gameObject;
        playerController = GetComponent<PlayerController>();
        playerMotor = GetComponent<PlayerMotor>();
        playerLantern = GetComponent<PlayerLantern>();
        playerData = GetComponent<PlayerData>();


        state.AddCallback("HP", OnHPChanged);
        if (entity.IsOwner)
        {
            state.MaxHP = 500;
            state.HP = state.MaxHP;
            state.IsDead = false;
        }

    }

    void OnHPChanged()
    {
        float perc = state.HP / state.MaxHP;
        HPFill.fillAmount = perc;

        if (state.HP <= 0)
            StartCoroutine(DeathAndRespawn());
    }

    public void InitHPBarColor(bool ally)
    {

        if (ally)
            HPFill.color = Color.cyan;
        else
            HPFill.color = Color.red;
    }

    public void UpdateHP(float delta_amount)
    {
        if (state.HP > 0 && state.HP <= state.MaxHP)

         state.HP += delta_amount;
        if (state.HP > state.MaxHP)
            state.HP = state.MaxHP;

        if (state.HP <= 0)
            state.IsDead = true;
    }

    public void ShowHPBar()
    {
        HPBar.SetActive(true);
    }

    IEnumerator DeathAndRespawn()
    {
        
        // Respawn Screen UI - Local Only
        if (entity.HasControl)
        {
            UIManager.instance.SetRespawnScreen(true);
            playerLantern.PlayerLanternLight.gameObject.SetActive(false);
            
        }

        // Disable Player's Name and HP UI
        PlayerCanvas.SetActive(false);
        // Disable Player's Minimap icon
        MinimapIcon.gameObject.SetActive(false);
        // Player Death Anim (Just Rotate Chicken to lie on side to show plasyer is dead)
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        playerMotor.LockRotation(Quaternion.Euler(0.0f, 0.0f, 90.0f));
        playerController.LOCK_PLAYER_MOVEMENT = true;

        // Start Respawn Timer
        if (entity.HasControl)
        {
            respawn_timer = RESPAWN_TIME;
            while (respawn_timer > 0)
            {
                respawn_timer -= Time.deltaTime;
                UIManager.instance.UpdateRespawnTimer((int)respawn_timer);
                yield return null;
            }

            PlayerRespawnEvent evnt = PlayerRespawnEvent.Create(GlobalTargets.Everyone);
            evnt.PlayerEntity = entity;
            evnt.Send();

        }
        
    }

    public void Respawn()
    {
        if (entity.HasControl)
        {
            UIManager.instance.SetRespawnScreen(false);
        }
        playerMotor.lock_rotation = false;
        playerController.LOCK_PLAYER_MOVEMENT = false;
        playerLantern.RefillLantern(100.0f);
        if (BoltNetwork.IsServer)
        {
            state.IsDead = false;
            state.HP = state.MaxHP;
        }
        playerController.SetToSpawn();
        PlayerCanvas.SetActive(true);
        // Enable Player's Minimap icon
        MinimapIcon.gameObject.SetActive(true);
    }
}
