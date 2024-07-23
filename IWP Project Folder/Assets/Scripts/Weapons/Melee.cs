using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class Melee : Weapon
{

    public Collider attack_col;
    public Canvas attackRangeUI;

    public AudioSource AttackSFX;

    private IPlayerState playerState;
    private Animator playerAnimator;

 
    private const float defaultAttackSpeed = 0.5f;

    private List<GameObject> InRangeList = new List<GameObject>();



    public override void Attached()
    {
        timer = 0.0f;
        ATTACK_ANIM_TIMER = 1.0f;
        DEFAULT_ATTACK_ANIM_TIME = 1.0f;

        state.OnDetach += Detach;

        if (entity.IsOwner)
        {
            state.MaxDurability = Weapon.MAX_DURABILITY;
            state.Durability = state.MaxDurability;
        }

        transform.localRotation = ref_stray_rot;
        base_damage = 0;

        LinkSprite();

    }

    public override void AttachToPlayer(Transform parent)
    {
        attack_col.enabled = true;
         
        transform.position = Vector3.zero;
        transform.SetParent(parent);
        transform.localPosition = ref_attached_pos;
        transform.localRotation = ref_attached_rot;

        playerEntity = parent.parent.GetComponent<BoltEntity>();
        playerAnimator = playerEntity.gameObject.GetComponent<Animator>();
        playerAnimator = playerEntity.gameObject.GetComponent<Animator>();
        playerAnimator.keepAnimatorControllerStateOnDisable = true;
        playerState = playerEntity.GetState<IPlayerState>();
        playerEntity.gameObject.GetComponent<PlayerData>().OnAttackSpeedChanged += OnAttackSpeedUpdated;
        OnAttackSpeedUpdated();

    }

    public override void UpdateWeapon()
    {
        switch (attack_state)
        {
            case WEAPON_STATE.IDLE:
                if (AttackKeyClicked())
                {
                    // Enable UI showing attack range
                    attackRangeUI.gameObject.SetActive(true);

                    attack_state = WEAPON_STATE.MOUSE_DOWN;
                }
                break;
            case WEAPON_STATE.MOUSE_DOWN:
                // Get DIrection based on mouse
                Vector3 mousePos = PlayerMouse.instance.GetCursorInWorldPos();
                Vector3 dir = mousePos - playerEntity.transform.position;
                dir.y = 0;
                dir.Normalize();

                // Update Attack Range UI
                attackRangeUI.transform.rotation = Quaternion.LookRotation(dir);
                if (Input.GetKeyUp(GameSettings.ATTACK_KEY))
                {
                    // Disable attack range UI
                    attackRangeUI.gameObject.SetActive(false);

                    // Start Attack
                    timer = ATTACK_ANIM_TIMER;

                    Quaternion m_rotation = Quaternion.LookRotation(dir);
                    LockPlayerRotation(m_rotation);
                    CallAttackEvent();

                    attack_state = WEAPON_STATE.ATTACKING;
                }
                else if (Input.GetKeyDown(GameSettings.CANCELATTACK_KEY))
                {
                    // Disable attack range Ui
                    attackRangeUI.gameObject.SetActive(false);

                    attack_state = WEAPON_STATE.IDLE;
                }
                break;
            case WEAPON_STATE.ATTACKING:
                // Update timer 
                if (timer > 0)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0)
                    {
                        attack_state = WEAPON_STATE.IDLE;
                    }
                }
                break;
            default:
                break;
        }
    }

    public override void Detach()
    {
        attack_col.enabled = false;

        gameObject.SetActive(true);

        playerEntity.gameObject.GetComponent<PlayerData>().OnAttackSpeedChanged -= OnAttackSpeedUpdated;
        transform.SetParent(null);
        transform.rotation = ref_stray_rot;
    }

    protected override void OnAttackSpeedUpdated()
    {
        float player_atkspd = playerEntity.GetState<IPlayerState>().AttackSpeedMultiplier;
        ATTACK_ANIM_TIMER = (1.0f / player_atkspd) * DEFAULT_ATTACK_ANIM_TIME;
        playerAnimator.SetFloat("AttackSpeed", player_atkspd * defaultAttackSpeed);
    }

    public override void Attack(Vector3 dir)
    {
        StartCoroutine(MeleeAttack());
    }

    // Only On Server
    IEnumerator MeleeAttack()
    {

        playerState.Eat();

        float temp_timer = ATTACK_ANIM_TIMER * 0.5f;

        while (temp_timer > 0)
        {
            temp_timer -= Time.deltaTime;
            yield return null;
        }

        InRangeList.RemoveAll(s => s == null);

        for (int i = 0; i < InRangeList.Count; i++)
        {
            GameObject go = InRangeList[i];
            PerformAction(go);
        }

        InRangeList.RemoveAll(s => s == null);

    }

    void CallAttackEvent()
    {
        AttackSFX.Play();

        WeaponAttackEvent evnt = WeaponAttackEvent.Create(GlobalTargets.OnlyServer);
        evnt.WeaponEntity = entity;
        evnt.Target = Vector3.zero; // unused so any value
        evnt.Send();

    }

    void LockPlayerRotation(Quaternion rotation)
    {
        LockPlayerRotationEvent evnt = LockPlayerRotationEvent.Create(GlobalTargets.Everyone);
        evnt.Player = playerEntity;
        evnt.Timer = ATTACK_ANIM_TIMER;
        evnt.TargetRotation = rotation;
        evnt.Send();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;
        if ((other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Breakable") ||
            other.gameObject.CompareTag("Turret") || other.gameObject.CompareTag("MainCrystal")
            || other.gameObject.CompareTag("Minion") || other.gameObject.CompareTag("Monster"))
            && !CheckIfInRangeList(other.gameObject) && !other.isTrigger)
        {
            InRangeList.Add(other.gameObject);
        }  
    }

    void OnTriggerExit(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;
        if ((other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Breakable") ||
                    other.gameObject.CompareTag("Turret") || other.gameObject.CompareTag("MainCrystal")
                    || other.gameObject.CompareTag("Minion") || other.gameObject.CompareTag("Monster")) 
                    && CheckIfInRangeList(other.gameObject))
        {
            InRangeList.Remove(other.gameObject);
        }
    }

    bool CheckIfInRangeList(GameObject go)
    {
        for (int i = 0; i < InRangeList.Count; i++)
        {
            if (go == InRangeList[i])
                return true;
        }
        return false;
    }
}
