using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class PlayerController : EntityBehaviour<IPlayerState>
{
    public bool LOCK_PLAYER_MOVEMENT = false;

    public Vector3 SpawnPos;
    public Quaternion SpawnRot;

    private PlayerMotor playerMotor;
   // private PlayerWeapons playerWeapons;
    private bool _forward;
    private bool _backward;
    private bool _left;
    private bool _right;
   

   // private bool _hasControl = false;


    public void Awake()
    {
        playerMotor = GetComponent<PlayerMotor>();
      //  playerWeapons = GetComponent<PlayerWeapons>();
        
    }

    public override void Attached()
    {

        Init(entity.HasControl);
        playerMotor.Init(entity.HasControl);
    }

    public void Init(bool isMine)
    {
        if (isMine)
        {
           
        }
    }

    private void Update()
    {
        if (GlobalGameState.FREEZE_UPDATES || LOCK_PLAYER_MOVEMENT)
            return;

        if (entity.HasControl)
            PollKeys();
    }

    private void PollKeys()
    {
        _forward = Input.GetKey(KeyCode.W);
        _backward = Input.GetKey(KeyCode.S);
        _left = Input.GetKey(KeyCode.A);
        _right = Input.GetKey(KeyCode.D);

       // _mouseScroll = playerWeapons.CanSwitchWeapon(Input.mouseScrollDelta.y != 0);
     
     
    }

    public override void SimulateController()
    {
        if (GlobalGameState.FREEZE_UPDATES || LOCK_PLAYER_MOVEMENT)
            return;
     
        IPlayerCommandInput input = PlayerCommand.Create();
        input.Up = _forward;
        input.Down = _backward;
        input.Right = _right;
        input.Left = _left;

        entity.QueueInput(input);

        playerMotor.ExecuteCommand(_forward, _backward, _left, _right);
    }


    public override void ExecuteCommand(Command command, bool resetState)
    {
        PlayerCommand cmd = (PlayerCommand)command;

        if (resetState)
        {
            playerMotor.SetState(cmd.Result.Position, cmd.Result.Rotation);
        }
        else
        {
            PlayerMotor.State motorState = new PlayerMotor.State();
            int weaponIndex = state.WeaponIndex;

            if (!entity.HasControl)
            {
                motorState = playerMotor.ExecuteCommand(
                cmd.Input.Up,
                cmd.Input.Down,
                cmd.Input.Left,
                cmd.Input.Right);

                cmd.Result.Position = motorState.position;
                cmd.Result.Rotation = motorState.rotation;

             
            }

           
           
        }
    }
  

    public void SetToSpawn()
    {
        transform.position = SpawnPos;
        transform.rotation = SpawnRot;
    }

   
}
 