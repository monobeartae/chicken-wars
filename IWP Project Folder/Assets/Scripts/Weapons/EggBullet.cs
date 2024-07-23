using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class EggBullet : EntityBehaviour<IEggBulletState>
{
    public GameObject ExplodeEffectPrefab;
    public Collider explosionRangeCollider;
    public GameObject IncomingAttackUIPrefab;
    private GameObject incomingAttackUI = null;


    public AudioSource LaunchSFX;
    public AudioSource ExplodeSFX;

    public static float GRAVITY = 9.8f;
    private Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);
    private Rigidbody rigidbody = null;

    private float damage;
    private bool collided = false;

    private BoltEntity playerEntity;


    public override void Attached()
    {
        rigidbody = GetComponent<Rigidbody>();

        state.OnExplode += Explode;
        state.SetTransforms(state.Transform, transform);

        state.AddCallback("Target", SpawnWarningUI);

        LaunchSFX.Play();
    }

    void SpawnWarningUI()
    {
       
        if (state.Team != PlayerData.myPlayer.GetState<IPlayerState>().Team)
        {
            incomingAttackUI = Instantiate(IncomingAttackUIPrefab, state.Target, Quaternion.Euler(90.0f, 0.0f, 0.0f));
        }
    }

    public void Init(int team_id, float damage, Vector3 initial_vel, Vector3 target_pos)
    {
        vel = initial_vel;
        state.Team = team_id;
        state.Target = new Vector3(target_pos.x,
                   0.1f, target_pos.z);
        this.damage = damage;
       
    }

    public void LinkPlayer(BoltEntity player)
    {
        playerEntity = player;
    }

    void FixedUpdate()
    {
        // Only Server Updates
        if (!BoltNetwork.IsServer)
            return;

        if (collided)
            return;

        // Move Bullet Accordimgly
        vel.y -= GRAVITY * Time.deltaTime;
        Vector3 deltaPos = vel * Time.deltaTime;
        rigidbody.MovePosition(transform.position + deltaPos);


    }

    private void OnTriggerEnter(Collider other)
    {
        // Only Server Checks
        if (!BoltNetwork.IsServer)
            return;

        if (!collided && other.gameObject.layer == LayerMask.NameToLayer("PlayableTerrain"))
        {
            collided = true;
            explosionRangeCollider.enabled = true;
            state.Explode();
        }
        else if (collided)
        {
            if (!other.isTrigger)
                AttackManager.Attack(other.gameObject, damage, state.Team, true, playerEntity);
        }
    }

    // Called locally for everyone
    void Explode()
    {
        ExplodeSFX.Play();
        StartCoroutine(WaitExplosionEnd());
    }

    IEnumerator WaitExplosionEnd()
    {
        Vector3 effect_origin = transform.position;
        effect_origin.y = 0;
        Instantiate(ExplodeEffectPrefab, effect_origin, Quaternion.identity);
        Vector3 pos = transform.position;
        pos.y = -1;
        transform.position = pos;
        if (incomingAttackUI != null)
            Destroy(incomingAttackUI);

        yield return new WaitForSeconds(1.0f);

        if (BoltNetwork.IsServer)
            BoltNetwork.Destroy(gameObject);
    }
}
