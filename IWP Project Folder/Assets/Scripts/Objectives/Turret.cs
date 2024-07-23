using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;


public class Turret : EntityBehaviour<ITurretState>
{

    public int TeamID;

    public AudioSource ShootSFX;

    public GameObject TurretUICanvas;
    public GameObject HPBar;
    public Image HPFill;
    public Image AttackRangeUI;
    public LineRenderer TargetLineUI;
    private GameObject TurretBody;
    public Image MinimapIcon;
    public TeamVisibilityArea teamVisibilityArea;
    private Animator animator;

    public Transform TurretHolePosition;

    public static float damage = 20.0f;

    private float ATTACK_INTERVAL = 2.5f;
    private float attack_timer = 1.0f;

    private float INTERACT_SHOW_UI_TIME = 3.5f;
    private float interact_timer = 0.0f;
    private bool in_range = false;

    private Color warningColor = new Color32(255, 0, 0, 30);
    private Color defaultColor = new Color32(255, 255, 255, 30);

    private List<GameObject> targetList = new List<GameObject>();
    public override void Attached()
    {
        state.OnFire += OnFire;
        
        state.AddCallback("HP", OnHPChanged);
        state.AddCallback("TargetEnemy", TargetSet);
        if (BoltNetwork.IsServer)
        {
            state.TargetEnemy = null;
            state.IsTeamSet = false;
        }

        ToggleUI(false);
        TurretBody = transform.GetChild(0).gameObject;
        animator = GetComponent<Animator>();
        StartCoroutine(InitTurretTeamUI());
    }

    IEnumerator InitTurretTeamUI()
    {
        while (!state.IsTeamSet)
            yield return null;

        int localPlayerTeam = PlayerData.myPlayer.GetState<IPlayerState>().Team;

        if (state.Team == localPlayerTeam)
            MinimapIcon.color = MinimapIconSettings.AllyIconColour;
        else
            MinimapIcon.color = MinimapIconSettings.EnemyIconColour;

        if (state.Team == 0)
            MinimapIcon.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        else
            MinimapIcon.transform.localRotation = Quaternion.Euler(180.0f, 0.0f, -90.0f);

        teamVisibilityArea.TeamID = state.Team;
    }

    

    void Update()
    {
        if (GlobalGameState.FREEZE_UPDATES || state.IsDead)
            return;

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

        if (state.TargetEnemy != null)
        {

            // Rotate turret to face target
            Vector3 targetDir = state.TargetEnemy.transform.position - transform.position;
            targetDir.y = 0;
            Quaternion m_Rotation = Quaternion.LookRotation(targetDir);
            TurretBody.transform.rotation = m_Rotation;
            // Set Target Ray UI
            TargetLineUI.transform.rotation = m_Rotation;
            Vector3 endPoint = TargetLineUI.GetPosition(0);
            float dis = targetDir.magnitude;
            endPoint.z = dis;
            TargetLineUI.SetPosition(1, endPoint);
        }

        if (!BoltNetwork.IsServer)
            return;

        if (state.TargetEnemy == null) // No Current Target
        {
            // Set New Target if there are targets in range
            if (targetList.Count > 0)
                SetNextTarget();
        }
        else // There is a current target
        {
            IEntityState entityState = state.TargetEnemy.GetState<IEntityState>();
            // If Target Has Left Range Or Died, Set New Target
            if (entityState.IsDead)
            {
                targetList.Remove(state.TargetEnemy.gameObject);
                SetNextTarget();
                return;
            }
            else if (!CheckIfInTargetList(state.TargetEnemy.gameObject))
            {
                SetNextTarget();
                return;
            }

           

            // ATTACK
            if (attack_timer > 0.0f)
            {
                attack_timer -= Time.deltaTime;
                if (attack_timer <= 0.0f)
                {
                    state.Fire();
                    attack_timer = ATTACK_INTERVAL;
                }
            }
        }
    }

    void OnHPChanged()
    {
        if (state.HP < state.MaxHP)
        {
            ToggleUI(true);
            interact_timer = INTERACT_SHOW_UI_TIME;
        }


        if (state.HP <= 0)
            Destroy();
    }

    void TargetSet()
    {
        if (state.TargetEnemy == null)
        {
            TargetLineUI.gameObject.SetActive(false);
            AttackRangeUI.color = defaultColor;
            return;
        }

        TargetLineUI.gameObject.SetActive(true);

        if (PlayerData.myPlayer == state.TargetEnemy)
            AttackRangeUI.color = warningColor;
        else
            AttackRangeUI.color = defaultColor;

    }

    void OnFire()
    {
        if (BoltNetwork.IsServer)
        {
            Vector3 spawnPos = TurretHolePosition.position;
            BoltEntity bulletEntity = BoltNetwork.Instantiate(BoltPrefabs.TurretBullet, spawnPos, Quaternion.identity);
            TurretBullet bullet = bulletEntity.gameObject.GetComponent<TurretBullet>();
            bullet.Init(state.Team, state.TargetEnemy);
        }
        ShootSFX.Play();
    }

    void Destroy()
    {
        StartCoroutine(DestroyTurret());
    }

    IEnumerator DestroyTurret()
    {
        if (BoltNetwork.IsServer)
        {
            state.IsDead = true;
        }

        animator.SetTrigger("Destroy");
        teamVisibilityArea.enabled = false;
        AttackRangeUI.gameObject.SetActive(false);
        MinimapIcon.gameObject.SetActive(false);
        HPBar.gameObject.SetActive(false);
        TargetLineUI.gameObject.SetActive(false);

        yield return new WaitForSeconds(5.0f);

        if (BoltNetwork.IsServer)
        {
            BoltNetwork.Destroy(gameObject);
        }
    }

    public void ToggleUI(bool on)
    {
        TurretUICanvas.SetActive(on);
    }

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

            if (BoltNetwork.IsServer)
            {
                if (state.Team != player.GetState<IPlayerState>().Team
                   && !CheckIfInTargetList(player.gameObject) && !other.isTrigger)
                {
                    targetList.Add(player.gameObject);
                }
            }
        }

        if (!BoltNetwork.IsServer)
            return;

        if (other.CompareTag("Minion"))
        {
            // Check if enemy
            IEntityState minion = other.GetComponent<BoltEntity>().GetState<IEntityState>();
            if (state.Team != minion.Team
                && !CheckIfInTargetList(other.gameObject) && !other.isTrigger)
            {
                targetList.Add(other.gameObject);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;

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


        // Update Target List
        if (!BoltNetwork.IsServer)
            return;

        if ((other.CompareTag("Player")
            || other.CompareTag("Minion"))
            && CheckIfInTargetList(other.gameObject)
            && !other.isTrigger)
        {
            targetList.Remove(other.gameObject);
        }
    }

    void SetNextTarget()
    {
        targetList.RemoveAll(s => s == null);

        if (targetList.Count == 0)
        {
            TargetLineUI.gameObject.SetActive(false);
            state.TargetEnemy = null;
            return;
        }

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

        state.TargetEnemy = target.GetComponent<BoltEntity>();
        TargetLineUI.gameObject.SetActive(true);
     
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
}
