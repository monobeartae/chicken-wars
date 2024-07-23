using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class EggLauncher : Weapon
{
    public Canvas attackRangeUI;

    private Vector3 bullet_spawn_offset = new Vector3(0.0f, 0.08f, 0.3f);

    public static float UPWARD_FORCE = 4.5f;
    private BulletPhysics bulletPhysics = new BulletPhysics();

    private float ATTACK_RANGE = 3.0f;

    public override void Attached()
    {
        base.Attached();

        timer = 0.0f;
        ATTACK_ANIM_TIMER = 1.0f;
        DEFAULT_ATTACK_ANIM_TIME = 2.0f;

        base_damage = 30;
        durability_cost = 20;
        weaponName = "Egg Launcher";

        LinkSprite();

    }

    public override void AttachToPlayer(Transform parent)
    {
        base.AttachToPlayer(parent);

        // Init Bullet Physics
        bulletPhysics.CalculateTimer(transform.position.y + bullet_spawn_offset.y);
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
                // Get Direction based on mouse
                Vector3 mousePos = PlayerMouse.instance.GetCursorInWorldPos();
                Vector3 attack_pos = mousePos;
                Vector3 temp = mousePos;
                temp.y = 0;
                Vector3 player_pos = playerEntity.transform.position;
                player_pos.y = 0;

                if (Vector3.Distance(temp, player_pos) > ATTACK_RANGE)
                {
                    attack_pos = player_pos + (temp - player_pos).normalized * ATTACK_RANGE;
                    attack_pos.y = mousePos.y;
                }
                // Update UI showing attack range
                attackRangeUI.transform.position = new Vector3(attack_pos.x,
                   0.01f, attack_pos.z);
               

                if (Input.GetKeyUp(GameSettings.ATTACK_KEY))
                {
                    // Disable attack range UI
                    attackRangeUI.gameObject.SetActive(false);

                    // Start Attack
                   
                    timer = ATTACK_ANIM_TIMER;
                    CallAttackEvent(attack_pos);

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
    protected override void OnAttackSpeedUpdated()
    {
        float player_atkspd = playerEntity.GetState<IPlayerState>().AttackSpeedMultiplier;
        ATTACK_ANIM_TIMER = (1.0f / player_atkspd) * DEFAULT_ATTACK_ANIM_TIME;
    }

    public override void Attack(Vector3 dir) // dir is the target location
    {
        Vector3 bulletSpawnPos = transform.position + bullet_spawn_offset;

        // Instantiate bullet prefab
        BoltEntity entity = BoltNetwork.Instantiate(BoltPrefabs.Egg, bulletSpawnPos, Quaternion.Euler(-90.0f, 0.0f, 0.0f));
        EggBullet egg = entity.gameObject.GetComponent<EggBullet>();

        // Set Bullet Velocity
        bulletPhysics.CalculateLaunchForce(bulletSpawnPos, dir);
        Vector3 bulletdir = (dir - bulletSpawnPos).normalized;
        Vector3 bullet_vel = new Vector3(bulletdir.x * bulletPhysics.LAUNCH_FORCE,
            UPWARD_FORCE,
            bulletdir.z * bulletPhysics.LAUNCH_FORCE);

        
        egg.Init(playerEntity.GetState<IPlayerState>().Team, CalculateDamage(), bullet_vel, dir);
        egg.LinkPlayer(playerEntity);

        state.Durability -= durability_cost;

    }

    void CallAttackEvent(Vector3 target)
    {
        if (!BoltNetwork.IsServer)
        {
            WeaponAttackEvent evnt = WeaponAttackEvent.Create(GlobalTargets.OnlyServer);
            evnt.WeaponEntity = entity;
            evnt.Target = target;
            evnt.Send();

        }
        else
        {
            Attack(target);
        }

    }

}

struct BulletPhysics
{
    //public float UPWARDS_FORCE;
    public float LAUNCH_FORCE;

    private float s;
    private float u;
    private float v;
    private float a;
    private float t;

    public void CalculateTimer(float heightFromGround)
    {
        // from launch upwards til max distance in air
        u = EggLauncher.UPWARD_FORCE;
        v = 0;
        a = -1 * EggBullet.GRAVITY;

        float timerA = (v - u) / a;
        s = 0.5f * (u + v) * timerA;
        float maxHeight = Mathf.Abs(s) + heightFromGround;
       
        // fall
        u = 0;
        a = EggBullet.GRAVITY;
        s = maxHeight;
        v = Mathf.Sqrt(2 * a * s);
        float timerB = (2 * s) / (u + v);

        t = timerA + timerB;
    }

    public void CalculateLaunchForce(Vector3 bullet_spawn_pos, Vector3 target_pos)
    {
        // u =>  t, s, a
        // s = ut + 1/2at^2
        // ut = s - 0.5f(at^2)
        Vector3 dis = target_pos - bullet_spawn_pos;
        dis.y = 0;
        s = dis.magnitude;
        a = 0.0f;

        u = (s - (0.5f * a * t * t)) / t;
        float kConstant = 1.0f;
        LAUNCH_FORCE = u * kConstant;


    }

}