using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class TurretBullet : EntityBehaviour<ITurretBulletState>
{
    private Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);
    private Rigidbody rigidbody = null;

    private float BULLET_SPEED = 2.0f;
    private float SPIN_SPEED = 700.0f;
    private float homing_accel_rate = 0.25f;


    public override void Attached()
    {
        rigidbody = GetComponent<Rigidbody>();
        state.SetTransforms(state.Transform, transform);
    }

    void FixedUpdate()
    {
        // Only Server Updates
        if (!BoltNetwork.IsServer)
            return;

        if (state.TargetEntity == null)
            return;

        Vector3 accel = (state.TargetEntity.transform.position - transform.position).normalized * homing_accel_rate;
        vel += accel;
        vel = vel.normalized * BULLET_SPEED;

        // Move Bullet Accordimgly
        Vector3 deltaPos = vel * Time.deltaTime;
        rigidbody.MovePosition(transform.position + deltaPos);
        // SPIN CHICKEN
        Quaternion rot = transform.rotation;
        float currAngle = rot.eulerAngles.y;
        currAngle += SPIN_SPEED * Time.deltaTime;
        if (currAngle > 360)
            currAngle -= 360;
        rot = Quaternion.Euler(rot.eulerAngles.x, currAngle, rot.eulerAngles.z);
        rigidbody.MoveRotation(rot);

    }

    public void Init(int team_id, BoltEntity targetEntity)
    {
        state.Team = team_id;
        state.TargetEntity = targetEntity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;

        BoltEntity go_entity = other.gameObject.GetComponent<BoltEntity>();
        if (!other.isTrigger && go_entity != null && go_entity == state.TargetEntity)
        {
            OnTargetHit(other.gameObject);
        }
    }

    private void OnTargetHit(GameObject go)
    {
        AttackManager.Attack(go, Turret.damage, state.Team, false, null);
        BoltNetwork.Destroy(gameObject);
    }
}
