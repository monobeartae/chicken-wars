using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class Weapon : EntityBehaviour<IWeaponState>
{

    public WeaponID ID = WeaponID.UNSET;
    public Sprite weaponIconSprite;

    public Vector3 ref_attached_pos;
    public Quaternion ref_attached_rot;
    public Quaternion ref_stray_rot;

    protected BoltEntity playerEntity;

    public bool is_stray = true;
    protected WEAPON_STATE attack_state = WEAPON_STATE.IDLE;

    public static float MAX_DURABILITY = 100.0f;
    protected float durability_cost = 0.0f;

    protected Animator animator;

    protected string weaponName;
    protected int base_damage;

    protected float timer;
    protected float ATTACK_ANIM_TIMER;
    protected float DEFAULT_ATTACK_ANIM_TIME;

    public static Sprite[] weaponSprites = new Sprite[(int)WeaponID.NUM_TOTAL];


    public override void Attached()
    {
        timer = ATTACK_ANIM_TIMER = DEFAULT_ATTACK_ANIM_TIME = 0.0f;

        state.OnDetach += Detach;

        if (entity.IsOwner)
        {
            state.MaxDurability = 100.0f;
            state.Durability = state.MaxDurability;
        }
        state.IsBroken = false;

        transform.localRotation = ref_stray_rot;

        if (ID != WeaponID.MELEE)
        {
            animator = GetComponent<Animator>();
            animator.enabled = false;
        }

    }

   
    public virtual void UpdateWeapon()
    {

    }



    public virtual void Attack(Vector3 dir)
    {

    }
    public void TriggerAnimation(string anim)
    {
        if (ID == WeaponID.MELEE)
            return;
        animator.SetTrigger(anim);
    }


    public virtual void AttachToPlayer(Transform parent)
    {
        if (ID != WeaponID.MELEE)
            animator.enabled = true;

        is_stray = false;

        transform.position = Vector3.zero;
        transform.SetParent(parent);
        transform.localPosition = ref_attached_pos;
        transform.localRotation = ref_attached_rot;

        playerEntity = parent.parent.GetComponent<BoltEntity>();
        playerEntity.gameObject.GetComponent<PlayerData>().OnAttackSpeedChanged += OnAttackSpeedUpdated;
        OnAttackSpeedUpdated();

    }

    public virtual void Detach()
    {
        gameObject.SetActive(true);

        is_stray = true;

        if (ID != WeaponID.MELEE)
            animator.enabled = false;

        transform.SetParent(null);
        transform.rotation = ref_stray_rot;
        playerEntity.gameObject.GetComponent<PlayerData>().OnAttackSpeedChanged -= OnAttackSpeedUpdated;
    }

    // Called by Server
    public void PerformAction(GameObject go)
    {
        int damage = CalculateDamage();
        AttackManager.Attack(go, damage, playerEntity.GetState<IPlayerState>().Team, true, playerEntity);
    }

    protected virtual void OnAttackSpeedUpdated()
    {

    }

    protected int CalculateDamage()
    {
        return  (int)(base_damage * TeamInventory.WEAPON_DAMAGE_MULTIPLIER + playerEntity.GetState<IPlayerState>().BaseDamage);
    }

    public BoltEntity GetOwner()
    {
        return playerEntity;
    }

    public float GetDurability()
    {
        return state.Durability;
    }

    public float GetMaxDurability()
    {
        return state.MaxDurability;
    }

    protected bool AttackKeyClicked()
    {
        return Input.GetKeyDown(GameSettings.ATTACK_KEY) && !UIManager.IsPointerOverUIElement();
    }

    public float GetAttackIntervalPerc()
    {
        if (timer <= 0.0f)
            return 0.0f;

        return timer / ATTACK_ANIM_TIMER;
    }
     
    public void CustomUpdateTimer()
    {
        // Update timer 
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                attack_state = WEAPON_STATE.IDLE;
            }
        }
    }

    public string GetName()
    {
        return weaponName;
    }

    protected void LinkSprite()
    {
        weaponSprites[(int)ID] = weaponIconSprite;
    }

    public static Sprite GetSpriteByID(WeaponID id)
    {
        return weaponSprites[(int)id];
    }
}

public enum WEAPONS
{
    FRYING_PAN,
    //RAY_GUN,
    EGG_LAUNCHER,

    NUM_TOTAL
}

public enum WeaponID
{
    UNSET,
    MELEE,
    FRYING_PAN,
    RAY_GUN,
    EGG_LAUNCHER,
    NUM_TOTAL
}

public enum WEAPON_STATE
{
    IDLE,
    MOUSE_DOWN,
    ATTACKING,

    NUM_TOTAL
}