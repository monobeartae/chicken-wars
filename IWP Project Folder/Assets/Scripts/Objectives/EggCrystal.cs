using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class EggCrystal : EntityBehaviour<IEggCrystalState>
{
    public Canvas UICanvas;
    public GameObject HPBar;
 //   public Image HPFill;

    private float INTERACT_SHOW_UI_TIME = 5.0f;
    private float interact_timer = 0.0f;
    private bool in_range = false;

 //   private float MAX_HP = 300;

    private Animator animator = null;

    public override void Attached()
    {
        animator = GetComponent<Animator>();

        state.AddCallback("HP", OnHPChanged);
       // state.AddCallback("Team", TeamIDSet);
       
        
        //if (entity.IsOwner)
        //{
        //    state.HP = MAX_HP;
      
        //}

        //StartCoroutine(InitCrystal());
    }

    public void TeamIDSet()
    {
       // StartCoroutine(InitCrystal());
    }
   
    IEnumerator InitCrystal()
    {
        // Wait for local Player's team to be assigned
        while (PlayerData.myPlayer == null)
            yield return null;
       

        // Set HP Bar Colour
        //SetHPColor();
    }

    void OnHPChanged()
    {

        if (state.HP < state.MaxHP)
        {
            ToggleUI(true);
            interact_timer = INTERACT_SHOW_UI_TIME;
        }

        if (state.HP <= 0)
            StartCoroutine(ExplodeCrystal());
    }

    void Update()
    {
        if (interact_timer > 0.0f)
        {
            interact_timer -= Time.deltaTime;
            if (interact_timer <= 0.0f)
            {
                interact_timer = 0.0f;
                if (!in_range)
                    ToggleUI(false);
            }
        }
    }

    IEnumerator ExplodeCrystal()
    {
        GameSceneManager.instance.SetCameraFocus(transform);
        GlobalGameState.FREEZE_UPDATES = true;
        HPBar.SetActive(false);
        animator.SetTrigger("Explode");
        UIManager.instance.DisableAllUI();
        yield return new WaitForSeconds(5.0f);

        bool myCrystal = PlayerData.myPlayer.GetState<IPlayerState>().Team == state.Team;
        //GameSceneManager.instance.GameEnd(!myCrystal);
        GameSettings.EndGame(!myCrystal);
    }

    //void SetHPColor()
    //{
    //    int myTeam = PlayerData.myPlayer.GetState<IPlayerState>().Team;
    //    bool ally = myTeam == state.Team;
    //    if (ally)
    //        HPFill.color = Color.cyan;
    //    else
    //        HPFill.color = Color.red;
    //}


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // if player enters turret range, show ui
            BoltEntity player = other.gameObject.GetComponent<BoltEntity>();
            if (player == PlayerData.myPlayer)
            {
                ToggleUI(true);
                in_range = true;
            }
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // if player exits turret range, hide ui
            BoltEntity player = other.gameObject.GetComponent<BoltEntity>();
            if (player == PlayerData.myPlayer)
            {
                ToggleUI(false);
                in_range = false;
            }

        }
    }

    public void ToggleUI(bool on)
    {
        UICanvas.gameObject.SetActive(on);
    }
}
