using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;


public class PlayerMotor : EntityBehaviour<IPlayerState>
{

    private Rigidbody rb = null;
    private PlayerData playerData = null;

    private const float TURN_SPEED = 20f;
    private const float PLAYER_SPEED = 1.2f;

    private Vector3 _lastServerPos = Vector3.zero;
    private bool _firstState = true;

    public bool lock_rotation = false;
    private Quaternion target_rotation;
    private float lock_timer = 0.0f;


    public static float HEIGHT_FROM_GROUND = 2.2f;
    private bool hasMovement = false;
    public bool playerHasMovement
    {
        get { return hasMovement; }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerData = gameObject.GetComponent<PlayerData>();

    }

    public override void Attached()
    {
        state.SetTransforms(state.Transform, transform);
        state.SetAnimator(GetComponent<Animator>());
        state.Animator.applyRootMotion = entity.HasControl;
    }

    public void Init(bool isMine)
    {

    }

    public State ExecuteCommand(bool forward, bool backward, bool left, bool right)
    {
        Vector3 m_Dir = Vector3.zero;
        if (forward ^ backward)
        {
            m_Dir += forward ? Vector3.forward : -Vector3.forward;
        }
        if (left ^ right)
        {
            m_Dir += right ? Vector3.right : Vector3.left;
        }

        m_Dir.Normalize();
        if (m_Dir.magnitude > 0)
        {
            hasMovement = true;
            state.Animator.SetBool("Walk", true);
        }
        else
        {
            hasMovement = false;
            state.Animator.SetBool("Walk", false);
        }


        rb.MovePosition(rb.position + m_Dir * PLAYER_SPEED * state.MovementSpeedMultiplier * Time.deltaTime);
        if (!lock_rotation)
        {
            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Dir, TURN_SPEED * Time.deltaTime, 0f);
            Quaternion m_Rotation = Quaternion.LookRotation(desiredForward);
            rb.MoveRotation(m_Rotation);
        }
        else
        {
            Quaternion m_Rotation = Quaternion.RotateTowards(transform.rotation, target_rotation, Mathf.Rad2Deg * TURN_SPEED * Time.deltaTime);
            rb.MoveRotation(m_Rotation);
        }

        if (lock_timer > 0)
        {
            lock_timer -= Time.deltaTime;
            if (lock_timer <= 0)
            {
                lock_rotation = false;
                lock_timer = 0.0f;
            }

        }

        State stateMotor = new State();
        stateMotor.position = transform.position;
        stateMotor.rotation = transform.rotation;

        return stateMotor;
    }

    public void SetState(Vector3 position, Quaternion rotation)
    {

        if (_firstState)
        {
            if (position != Vector3.zero)
            {
                transform.position = position;
                _firstState = false;
                _lastServerPos = Vector3.zero;
            }
        }
        else
        {
            if (position != Vector3.zero)
            {
                _lastServerPos = position;
            }

            transform.position += (_lastServerPos - transform.position) * 0.5f;
        }
    }

    public void LockRotation(Quaternion target, float timer)
    {
        lock_timer = timer;
        lock_rotation = true;
        target_rotation = target;

    }
    public void LockRotation(Quaternion target)
    {
        lock_rotation = true;
        target_rotation = target;

    }

    public struct State
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}
