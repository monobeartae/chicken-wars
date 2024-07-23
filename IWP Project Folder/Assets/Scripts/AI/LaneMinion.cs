using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

using Photon.Bolt;

public class LaneMinion : EntityBehaviour<IMinionState> // ori minion speed was 0.3
{
    // Minion Light
    public Light MinionPointLight;

    // Minion UI
    public GameObject MinionUICanvas;
    public GameObject HPBar;
    public Image HPFill;
    public Image MinimapIcon;
    public TeamVisibilityArea teamVisibilityArea;

    // UI vars
    private float INTERACT_SHOW_UI_TIME = 5.0f;
    private float interact_timer = 0.0f;
    private bool in_range = false;
    private Color enemyHPColor = new Color32(255, 100, 100, 255);
    private Color allyHPColor = new Color32(100, 175, 175, 255);

    // Minion Stats
    private float damage = 3.0f;
    
    // Minion FSM AND Attack Things
    private MINION_STATE minionState = MINION_STATE.NAVIGATING;
    private float ATTACK_RANGE;
    private float ATTACK_INTERVAL = 2.0f;
    private float attack_timer = 0.5f;
   
    // Minion State Related Things
    private List<GameObject> targetList = new List<GameObject>();

    // Navigation
    private NavMeshAgent navMeshAgent;
    private const float NAV_PATH_CALCULATE_INTERVAL = 1.0f;
    private float nav_reset_timer = NAV_PATH_CALCULATE_INTERVAL;

    public override void Attached()
    {
        state.AddCallback("HP", OnHPChanged);
        state.AddCallback("Team", OnTeamSet);
        state.SetTransforms(state.Transform, transform);

        state.SetAnimator(GetComponent<Animator>());
        //state.Animator.applyRootMotion = entity.IsOwner;
        state.Animator.SetBool("Walk", true);


        navMeshAgent = GetComponent<NavMeshAgent>();
        ATTACK_RANGE = navMeshAgent.stoppingDistance;

        OnTeamSet();
    }


    void OnTeamSet()
    {
        int myTeam = PlayerData.myPlayer.GetState<IPlayerState>().Team;
        if (myTeam == state.Team)
        {
            HPFill.color = allyHPColor;
            MinimapIcon.color = MinimapIconSettings.AllyIconColour;
        }
        else
        {
            HPFill.color = enemyHPColor;
            MinimapIcon.color = MinimapIconSettings.EnemyIconColour;
        }
        teamVisibilityArea.TeamID = state.Team;
    }
    public void Init(int team_id, List<GameObject> obj_list)
    {
        state.Team = team_id;
        foreach (GameObject obj in obj_list)
        {
            targetList.Add(obj);
        }


        SetNewTargetEntity();
        minionState = MINION_STATE.NAVIGATING;
    }

    void Update()
    {
        if (GlobalGameState.FREEZE_UPDATES || state.IsDead)
            return;

        // Minion UI
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

        if (!BoltNetwork.IsServer)
            return;

        if (state.TargetEntity == null)
            SetNewTargetEntity();
        else
        {
            IEntityState targetEntity = state.TargetEntity.GetState<IEntityState>();
            if (targetEntity.IsDead)
            {
                targetList.Remove(state.TargetEntity.gameObject);
                SetNewTargetEntity();
            }
        }

        switch (minionState)
        {
            case MINION_STATE.ATTACKING:
                if (attack_timer > 0.0f)
                {
                    attack_timer -= Time.deltaTime;
                    if (attack_timer <= 0.0f)
                    {
                        Attack();
                        attack_timer = ATTACK_INTERVAL;
                    }
                }
                Vector3 dis = transform.position - state.TargetEntity.transform.position;
                dis.y = 0;
                if (dis.magnitude > ATTACK_RANGE)
                {
                    SetNewTargetEntity();
                    state.Animator.SetBool("Walk", true);
                    minionState = MINION_STATE.NAVIGATING;
                }
                break;
            case MINION_STATE.NAVIGATING:
                if (ReachedDestination())
                {
                    minionState = MINION_STATE.ATTACKING;
                    state.Animator.SetBool("Walk", false);
                    break;
                }
                nav_reset_timer -= Time.deltaTime;
                if (nav_reset_timer <= 0.0f)
                {
                    nav_reset_timer = NAV_PATH_CALCULATE_INTERVAL;
                    navMeshAgent.SetDestination(state.TargetEntity.transform.position);
                }
                break;
        }
    }

    void Attack()
    {
        state.Run();

        GameObject go = state.TargetEntity.gameObject;
        AttackManager.Attack(go, damage, state.Team, false, entity);

    }

    void OnHPChanged()
    {
        if (state.HP < state.MaxHP)
        {
            ToggleUI(true);
            interact_timer = INTERACT_SHOW_UI_TIME;
        }

        if (state.HP <= 0)
            Die();
    }

    void Die()
    {
        StartCoroutine(MinionDeath());
    }

    IEnumerator MinionDeath()
    {
        navMeshAgent.enabled = false;
        state.Animator.SetBool("Walk", false);
        minionState = MINION_STATE.DEAD;
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        MinionPointLight.enabled = false;
        teamVisibilityArea.enabled = false;
        MinimapIcon.gameObject.SetActive(false);
        HPBar.gameObject.SetActive(false);

        yield return new WaitForSeconds(3.0f);

        if (BoltNetwork.IsServer)
            BoltNetwork.Destroy(gameObject);
    }

    public void ToggleUI(bool on)
    {
        MinionUICanvas.SetActive(on);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // if player enters minion range, show ui
            BoltEntity player = other.gameObject.GetComponent<BoltEntity>();
            if (player == PlayerData.myPlayer)
            {
                ToggleUI(true);
                in_range = true;
            }

            // Update Target List
            if (BoltNetwork.IsServer)
            {
                // Check if enemy
                if (state.Team != player.GetState<IPlayerState>().Team
                    && !CheckIfInTargetList(player.gameObject))
                {
                    targetList.Add(player.gameObject);
                    if (minionState == MINION_STATE.NAVIGATING)
                        SetNewTargetEntity();
                }
            }
        }

        // Update Target List
        if (!BoltNetwork.IsServer)
            return;

        if (other.CompareTag("Minion"))
        {
            // Check if enemy
            IEntityState minion = other.GetComponent<BoltEntity>().GetState<IEntityState>();
            if (state.Team != minion.Team
                && !CheckIfInTargetList(other.gameObject))
            {
                targetList.Add(other.gameObject);
                if (minionState == MINION_STATE.NAVIGATING)
                    SetNewTargetEntity();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // if player exits minion range, hide ui
            BoltEntity player = other.gameObject.GetComponent<BoltEntity>();
            if (player == PlayerData.myPlayer)
            {
                ToggleUI(false);
                in_range = false;
            }
        }

        // Update Target List
        if (!BoltNetwork.IsServer)
            return;

        if ((other.CompareTag("Player")
            || other.CompareTag("Minion"))
            && CheckIfInTargetList(other.gameObject))
        {
            targetList.Remove(other.gameObject);
        }

    }

  
    void SetNewTargetEntity()
    {
        // Safety
        targetList.RemoveAll(s => s == null);

        // Get Nearest Objective
        GameObject target = null;
        foreach (GameObject go in targetList)
        {
            if (target == null)
            {
                target = go;
            }
            else
            {
                float currDis = Vector3.Distance(target.transform.position, transform.position);
                float goDis = Vector3.Distance(go.transform.position, transform.position);
                if (goDis < currDis)
                    target = go;
            }
        }
        state.TargetEntity = target.GetComponent<BoltEntity>();
        Vector3 targetPos = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(targetPos);

    }

    bool CheckIfInTargetList(GameObject go)
    {
        for (int i = 0; i < targetList.Count; i++)
        {
            if (go == targetList[i])
                return true;
        }
        return false;
    }

    bool ReachedDestination()
    {
        if (!navMeshAgent.hasPath)
            return false;

        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            navMeshAgent.isStopped = true;
            return true;
        }

        return false;
    }
}

enum MINION_STATE
{
    NAVIGATING,
    ATTACKING,
    DEAD,

    NUM_TOTAL
}