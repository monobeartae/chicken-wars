using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class FryingPan : Weapon
{

    public Canvas attackRangeUI;
    public AudioSource AttackSFX;

    private SphereCollider pickupCol;
    private BoxCollider attackCol;


    public override void Attached()
    {
        timer = 0.0f;
        ATTACK_ANIM_TIMER = 2.0f;
        DEFAULT_ATTACK_ANIM_TIME = 2.0f;

        state.OnDetach += Detach;

        if (entity.IsOwner)
        {
            state.MaxDurability = 100.0f;
            state.Durability = state.MaxDurability;
        }

        base_damage = 10;
        durability_cost = 5;
        weaponName = "Frying Pan";
        transform.GetChild(0).localRotation = ref_stray_rot;

        animator = GetComponent<Animator>();
        animator.enabled = false;
        animator.keepAnimatorControllerStateOnDisable = true;

        pickupCol = GetComponent<SphereCollider>();
        attackCol = transform.GetChild(0).GetComponent<BoxCollider>();

        LinkSprite();

    }

    public override void AttachToPlayer(Transform parent)
    {
        animator.enabled = true;
        pickupCol.enabled = false;

        is_stray = false;

        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.GetChild(0).localPosition = ref_attached_pos;
        transform.GetChild(0).localRotation = ref_attached_rot;

        playerEntity = parent.parent.GetComponent<BoltEntity>();
        playerEntity.gameObject.GetComponent<PlayerData>().OnAttackSpeedChanged += OnAttackSpeedUpdated;
        OnAttackSpeedUpdated();
    }

    public override void Detach()
    {
        gameObject.SetActive(true);

        is_stray = true;

        animator.enabled = false;
        pickupCol.enabled = true;
        attackCol.enabled = false;

        playerEntity.gameObject.GetComponent<PlayerData>().OnAttackSpeedChanged -= OnAttackSpeedUpdated;
        transform.SetParent(null);
        transform.rotation = ref_stray_rot;
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

                // Update UI showing attack range
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
                        // End Attack
                        attack_state = WEAPON_STATE.IDLE;
                    }
                }
                break;
            default:
                break;
        }

    }


    public override void Attack(Vector3 dir)
    {
        StartCoroutine(FryingPanAttack());
        if (BoltNetwork.IsServer)
            state.Durability -= durability_cost;
    }

    //On Everyone's Side
    IEnumerator FryingPanAttack()
    {
       
        animator.SetTrigger("Attack");

        AttackSFX.Play();
        attackCol.enabled = true;
        yield return new WaitForSeconds(ATTACK_ANIM_TIMER * 0.5f);
        AttackSFX.Play();
        yield return new WaitForSeconds(ATTACK_ANIM_TIMER * 0.5f);
        attackCol.enabled = false;
    }
    public void OnPanCollided(GameObject go)
    {
      
        PerformAction(go);
    }
    protected override void OnAttackSpeedUpdated()
    {
        float player_atkspd = playerEntity.GetState<IPlayerState>().AttackSpeedMultiplier;
        ATTACK_ANIM_TIMER = (1.0f / player_atkspd) * DEFAULT_ATTACK_ANIM_TIME;
        animator.SetFloat("AttackSpeed", player_atkspd);
    }
    void CallAttackEvent()
    {
        WeaponAttackEvent evnt = WeaponAttackEvent.Create(GlobalTargets.Everyone);
        evnt.WeaponEntity = entity;
        evnt.Target = Vector3.zero;
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
}
