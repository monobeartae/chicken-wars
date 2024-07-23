using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using Photon.Bolt;

public class Metalon : EntityBehaviour<IMonsterState> // TBC - if more shit fucks up, separate a MetalonSpawner .cs instead for before attachment
{

    public static int METAL_BAR_DROP_AMT = 225; //45
    public static int CRYSTAL_ORE_DROP_AMOUNT = 200; //15

    // GO Vars
    public GameObject MetalonUICanvas, HPBar;
    public GameObject minimapIcon;
    private NavMeshAgent navMeshAgent;
    public Vector3 defaultScale, defaultPos;
    private Quaternion defaultRot;
    // UI vars
    private float INTERACT_SHOW_UI_TIME = 3.0f;
    private float interact_timer = 0.0f;
    // Metalon Stats
    private float damage = 5.0f;
    // FSM
    private METALON_STATE metalonState = METALON_STATE.IDLE;
    private float ATTACK_INTERVAL = 2.0f;
    private float attack_timer = 0.5f;
    // Navigation
    private const float NAV_PATH_CALCULATE_INTERVAL = 0.5f;
    private float nav_reset_timer = NAV_PATH_CALCULATE_INTERVAL;
    private bool in_range = false;
    // utility vars
    private float RESPAWN_TIMER = 30.0f;
    private float respawn_timer = 0.0f;

    private float DEATH_ANIM_TIMER = 1.0f;
    private float RESPAWN_ANIM_TIMER = 0.6f;


    // Called by initial entity to spawn actual metalon
    public void SpawnAndAttach()
    {
        if (BoltNetwork.IsServer)
        {
            BoltEntity metalon = BoltNetwork.Instantiate(BoltPrefabs.Metalon, transform.position, transform.rotation);
            metalon.transform.localScale = transform.localScale;
        }
        Destroy(gameObject);
    }

    public override void Attached()
    {
        state.AddCallback("HP", OnHPChanged);
        state.AddCallback("StateID", OnStateChanged);

        state.SetAnimator(GetComponent<Animator>());
        state.SetTransforms(state.Transform, transform);

        if (BoltNetwork.IsServer)
        {
            state.StateID = (int)metalonState;
            state.TargetEntity = null;
        }

        navMeshAgent = GetComponent<NavMeshAgent>();

        defaultScale = transform.localScale;
        defaultPos = transform.position;
        defaultRot = transform.rotation;
    }

    void OnHPChanged()
    {
        if (state.HP < state.MaxHP)
        {
            ToggleUI(true);
            interact_timer = INTERACT_SHOW_UI_TIME;
        }

        if (state.HP <= 0)
        {
            Die();
        }
    }

    void OnStateChanged()
    {
        metalonState = (METALON_STATE)state.StateID;

        switch (metalonState)
        {
            case METALON_STATE.IDLE: // RESPAWN
                StartCoroutine(StartRespawn());
                break;
            case METALON_STATE.DEAD: // DEAD
                StartCoroutine(StartDying());
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if (GlobalGameState.FREEZE_UPDATES)
            return;

        // Metalon UI
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

        Vector3 dis;
        switch (metalonState)
        {
            case METALON_STATE.DEAD:
                if (respawn_timer > 0.0f)
                {
                    respawn_timer -= Time.deltaTime;
                    if (respawn_timer <= 0.0f)
                    {
                        respawn_timer = 0.0f;
                        state.StateID = (int)METALON_STATE.IDLE;
                    }
                }
                break;
            case METALON_STATE.IDLE:
                if (state.TargetEntity != null)
                {
                    metalonState = METALON_STATE.NAVIGATING;
                    NavigateToTarget();
                }
                break;
            case METALON_STATE.NAVIGATING:
                // Close enoguh to player to attack
                if (ReachedDestination() && state.TargetEntity != null)
                {
                    state.WalkForward = false;
                    navMeshAgent.isStopped = true;
                    metalonState = METALON_STATE.ATTACKING;
                    break;
                }
                // Recalculate Path
                nav_reset_timer -= Time.deltaTime;
                if (nav_reset_timer <= 0.0f)
                {
                    nav_reset_timer = NAV_PATH_CALCULATE_INTERVAL;
                    if (state.TargetEntity != null)
                        navMeshAgent.SetDestination(state.TargetEntity.transform.position);
                    else
                        navMeshAgent.SetDestination(defaultPos);
                }

                // Check distance from default spawn
                Vector3 offset = transform.position - defaultPos;
                offset.y = 0;
                if (state.TargetEntity != null && offset.magnitude > 3.0f)
                    ReturnToSpawn();
                else if (state.TargetEntity == null && offset.magnitude <= 0.05f)
                {
                    transform.rotation = defaultRot;
                    metalonState = METALON_STATE.IDLE;
                    state.WalkForward = false;
                }

                // Check Distance from target Entity
                if (state.TargetEntity != null)
                {
                    dis = state.TargetEntity.transform.position - transform.position;
                    dis.y = 0;
                    if (dis.magnitude > 4.0f)
                        ReturnToSpawn();
                }
                break;
            case METALON_STATE.ATTACKING:
                // Check if Player is dead
                IPlayerState playerState = state.TargetEntity.GetState<IPlayerState>();
                if (playerState.IsDead)
                {
                    ReturnToSpawn();
                    metalonState = METALON_STATE.NAVIGATING;
                }
                // Attack
                if (attack_timer > 0.0f)
                {
                    attack_timer -= Time.deltaTime;
                    if (attack_timer <= 0.0f)
                    {
                        Attack();
                        attack_timer = ATTACK_INTERVAL;
                    }
                }
                // Rotate to face player
                Vector3 targetDir = state.TargetEntity.transform.position - transform.position;
                targetDir.y = 0;
                Quaternion m_Rotation = Quaternion.LookRotation(targetDir);
                transform.rotation = m_Rotation;
                // Check if player is too far away
                dis = state.TargetEntity.transform.position - transform.position;
                dis.y = 0;
                if (dis.magnitude > 2.0f)
                {
                    metalonState = METALON_STATE.NAVIGATING;
                    NavigateToTarget();
                }
                break;

            default:
                break;
        }
    }

    private void Attack()
    {
        StartCoroutine(MetalonAttack());
    }

    IEnumerator MetalonAttack()
    {
        state.SmashAttack();

        yield return new WaitForSeconds(0.5f);

        GameObject go = state.TargetEntity.gameObject;
        AttackManager.Attack(go, damage, -1, false, entity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (state.IsDead)
                return;

            // if player enters metalon range, show ui
            BoltEntity player = other.gameObject.GetComponent<BoltEntity>();
            if (player == PlayerData.myPlayer)
            {
                ToggleUI(true);
                in_range = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // if player exits metalon range, hide ui
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
        MetalonUICanvas.SetActive(on);
    }

    // Death Function called by Server
    void Die()
    {
        if (BoltNetwork.IsServer)
        {
            state.StateID = (int)METALON_STATE.DEAD;
            state.IsDead = true;
            respawn_timer = RESPAWN_TIMER;
        }
    }

    // Local Death Coroutine for all players
    IEnumerator StartDying()
    {
        HPBar.SetActive(false);
        minimapIcon.SetActive(false);
        navMeshAgent.enabled = false;

        if (BoltNetwork.IsServer)
        {
            state.Die();
            state.TargetEntity = null;
            state.WalkForward = false;
        }

        yield return new WaitForSeconds(DEATH_ANIM_TIMER);

        gameObject.transform.localScale = Vector3.zero;

    }

    // Local Respawn Coroutine for all players
    IEnumerator StartRespawn()
    {
        gameObject.transform.localScale = defaultScale;
        HPBar.SetActive(true);
        minimapIcon.SetActive(true);
        navMeshAgent.enabled = true;


        if (BoltNetwork.IsServer)
        {
            transform.position = defaultPos;
            transform.rotation = defaultRot;
            state.CastSpell();

            yield return new WaitForSeconds(RESPAWN_ANIM_TIMER);

            state.IsDead = false;
            state.HP = state.MaxHP;
        }
    }

    public void SetTargetEntity(BoltEntity player)
    {
        state.TargetEntity = player;
    }

    private bool ReachedDestination()
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

    private void ReturnToSpawn()
    {
        state.TargetEntity = null;
        state.WalkForward = true;
        navMeshAgent.isStopped = false;
        navMeshAgent.stoppingDistance = 0.0f;
        navMeshAgent.SetDestination(defaultPos);
    }

    private void NavigateToTarget()
    {
        state.WalkForward = true;
        navMeshAgent.isStopped = false;
        navMeshAgent.stoppingDistance = 1.3f;
        navMeshAgent.SetDestination(state.TargetEntity.transform.position);
    }
}
enum METALON_STATE
{
    IDLE,
    NAVIGATING,
    ATTACKING,
    DEAD,

    NUM_TOTAL
}