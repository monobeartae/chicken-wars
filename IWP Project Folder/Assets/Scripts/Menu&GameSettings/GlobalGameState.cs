using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;


using Photon.Bolt;
using Photon.Bolt.Matchmaking;

public class GlobalGameState : EntityEventListener<IGameState>
{
    #region Singleton
    public static GlobalGameState instance = null;
    #endregion
    public const float STARTMATCHCOUNTDOWN_TIMER = 5.0f;
    public static bool FREEZE_UPDATES = false;


    public static GAME_STAGE gameStage = GAME_STAGE.WAITING_ROOM;
    public static int ready_count = 0;
    private bool[] room_availability = new bool[6] { true, true, true, true, true, true };


    public override void Attached()
    {
        instance = this;


        if (entity.HasControl)
        {
            state.NumPlayersInRoom = 0;
            state.Timer = 0;
            state.Stage = (int)gameStage;
            for (int i = 0; i < RoomManager.MAX_PLAYERS; i++)
            {
                state.IsReady[i].isReady = false;
            }
        }

        state.AddCallback("NumPlayersInRoom", UpdatePlayerCount);
        state.AddCallback("Stage", UpdateGameState);
        state.AddCallback("IsReady[]", OnReadyCallback);
    }

    void Update()
    {
        if (!BoltNetwork.IsServer)
            return;

        switch (gameStage)
        {
            case GAME_STAGE.IN_GAME:
                state.Timer += Time.deltaTime;
                break;
            default:
                break;

        }
    }

    // Called when a New Player has loaded in
    public int PlayerJoinedRoom()
    {

        state.NumPlayersInRoom = Enumerable.Count<BoltConnection>(BoltNetwork.Clients) + 1;
        int player_room_id = GetAvailableSpot();
        if (player_room_id != -1)
        {
            room_availability[player_room_id] = false;
        }
        return player_room_id;
    }

    // Called Whenever Number of Players In Room is Updated
    public void UpdatePlayerCount()
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
            return;

        RoomManager.instance.UpdateWaitingInfo();

    }

    public static void PauseAllGameUpdates()
    {
        FREEZE_UPDATES = true;
    }
    public static void ResumeAllGameUpdates()
    {
        FREEZE_UPDATES = false;
    }

    private int GetAvailableSpot()
    {
        for (int i = 0; i < 6; i++)
        {
            if (room_availability[i])
            {
                return i;
            }
        }
        return -1;
    }

    void UpdateGameState()
    {
        gameStage = (GAME_STAGE)state.Stage;
    }

    void OnReadyCallback(IState state, string propertyPath, ArrayIndices arrayIndices)
    {
        int index = arrayIndices[0];
        RoomManager.instance.SetReadyUI(index, this.state.IsReady[index].isReady);
    }

    public void SetReady(int id)
    {
        state.IsReady[id].isReady = true;
    }

    public void PlayerInitReady()
    {
        ready_count++;

        if (ready_count == RoomManager.CURR_NUM_PLAYERS)
        {

            if (gameStage == GAME_STAGE.START_GAME_INIT)
            {
                gameStage = (GAME_STAGE)((int)gameStage + 1);
                state.Stage = (int)gameStage;
                ready_count = 0;

                StartGameCountdown();
            }
            // else if (gameStage == GAME_STAGE.START_GAME_INIT)
            // {
            //     RoomManager.instance.StartGame();
            // }
        }
    }

    public void HostStartGame()
    {
        gameStage = (GAME_STAGE)((int)gameStage + 1);
        state.Stage = (int)gameStage;
        ready_count = 0;
        RoomManager.instance.StartGame();

    }

    IEnumerator CountdownTimer()
    {
        state.Timer = STARTMATCHCOUNTDOWN_TIMER;

        while (state.Timer > 0)
        {
            state.Timer -= Time.deltaTime;
            yield return null;
        }
        state.Timer = 0;
        gameStage = (GAME_STAGE)((int)gameStage + 1);
        state.Stage = (int)gameStage;

    }

    public void StartGameCountdown()
    {
        StartCoroutine(CountdownTimer());
    }

}

public enum GAME_STAGE
{
    WAITING_ROOM,
    START_GAME_INIT,
    START_GAME_TIMER,
    IN_GAME,
    GAME_ENDED,

    NUM_TOTAL
}