using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;
using Photon.Bolt.Matchmaking;

public class RoomManager : GlobalEventListener
{
    #region Singleton
    public static RoomManager instance = null;
    #endregion

    public static int MAX_PLAYERS = 6;
    public static int CURR_NUM_PLAYERS = 0;

    public GameObject UI_WaitingMenu;
    public Text Text_WaitingDisplay;
    public Button readyButton;
    public Text readyButtonText;

    public Text roomInfo_text;

    public GameObject[] readyText;

    private int currentMap;

    Vector3[] spawnPos = new Vector3[6]
    {
        new Vector3(-1.5f, 0.0f, -3.0f),
        new Vector3(1.5f, 0.0f, -3.0f),
        new Vector3(-2.5f, 0.0f, -2.5f),
        new Vector3(2.5f, 0.0f, -2.5f),
        new Vector3(-2.5f, 0.0f, -3.5f),
        new Vector3(2.5f, 0.0f, -3.5f)
    };

    void Start()
    {
        instance = this;
        GlobalGameState.PauseAllGameUpdates();
        roomInfo_text.text = BoltMatchmaking.CurrentSession.HostName;

        currentMap = 0;
    }

    // Received by Server Whenever a new player joins the room and is loaded into the scene
    public override void OnEvent(PlayerJoinRoomEvent evnt)
    {

        // Update Server Global Game State Data
        int assigned_id = GlobalGameState.instance.PlayerJoinedRoom();

        // Spawn Player 
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        BoltEntity player = BoltNetwork.Instantiate(BoltPrefabs.Chicken, spawnPos[assigned_id], rotation);
        player.AssignControl(evnt.RaisedBy);
        player.GetState<IPlayerState>().ID = assigned_id;

        if (assigned_id % 2 == 0)
        {
            player.GetState<IPlayerState>().Team = (int)TEAM_ID.TEAM_A;
        }
        else
            player.GetState<IPlayerState>().Team = (int)TEAM_ID.TEAM_B;
        player.GetState<IPlayerState>().TeamSet = true;

        CURR_NUM_PLAYERS++;

        UpdateHostStartButton();
    }



    // Called when Scene has loaded on Local Machine
    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        Text_WaitingDisplay.text = "Joining Room...";
        StartCoroutine(JoinRoomInits());

    }


    IEnumerator JoinRoomInits()
    {
        // SPAWN PLAYER
        if (!BoltNetwork.IsServer)
        {
            // Send event to everyone upon loading into scene to tell everyone a new player has joined and for server to spawn player
            CallPlayerJoinRoomEvent();
        }
        else
        {
            // Update Server Global Game State Data
            int assigned_id = GlobalGameState.instance.PlayerJoinedRoom();

            // Spawn Player 
            Quaternion rotation = Quaternion.Euler(new Vector3(0, 180, 0));
            BoltEntity player = BoltNetwork.Instantiate(BoltPrefabs.Chicken, spawnPos[assigned_id], rotation);
            player.TakeControl();
            player.GetState<IPlayerState>().ID = assigned_id;

            // Assign Team Accordingly
            if (assigned_id % 2 == 0)
                player.GetState<IPlayerState>().Team = (int)TEAM_ID.TEAM_A;
            else
                player.GetState<IPlayerState>().Team = (int)TEAM_ID.TEAM_B;
            player.GetState<IPlayerState>().TeamSet = true;

            // On Server Side, Player is Attached faster than Control is Assigned, Player Name does not get sent so for now I Re - Init it; ; ;
            player.GetComponent<PlayerData>().InitPlayerData();

            CURR_NUM_PLAYERS++;

            Color buttonColor = readyButton.GetComponent<Image>().color;
            buttonColor.a *= 0.5f;
            readyButton.GetComponent<Image>().color = buttonColor;
            readyButtonText.text = "START";
        }

        // wait for local player attach
        while (PlayerData.myPlayer == null)
            yield return null;

        readyButton.gameObject.SetActive(true);


    }


    // Update UI
    public void UpdateWaitingInfo()
    {
        // Update Local Machine UI with GlobalGameState Data Controlled by Server
        Text_WaitingDisplay.text = "Waiting for Players... (" + GlobalGameState.instance.state.NumPlayersInRoom + "/" + MAX_PLAYERS + ")";
    }

    public void StartGame()
    {
        if (currentMap == -1) // RANDOM
            GlobalGameState.instance.state.MapID = GenerateRandomMapIndex();
        else
            GlobalGameState.instance.state.MapID = currentMap;

        CallStartGameEvent();
        BoltNetwork.LoadScene("GameScene");
    }

    public void LeaveRoom()
    {
        // TBC...
        // LeaveRoom Cleanup
    }


    public void UpdateHostStartButton()
    {
        readyButton.interactable = GlobalGameState.ready_count == CURR_NUM_PLAYERS - 1;
    }
    public void CallPlayerJoinRoomEvent()
    {
        PlayerJoinRoomEvent evnt = PlayerJoinRoomEvent.Create(GlobalTargets.OnlyServer);
        evnt.Send();
    }

    public void CallStartGameEvent()
    {
        StartGameEvent evnt = StartGameEvent.Create(GlobalTargets.Everyone);
        evnt.Send();
    }

    private int GenerateRandomMapIndex()
    {
        Array values = GAME_MAPS.GetValues(typeof(GAME_MAPS));
        System.Random random = new System.Random();
        GAME_MAPS randomMap = (GAME_MAPS)values.GetValue(random.Next(values.Length));
        return (int)randomMap;
    }

    public override void OnEvent(SendPlayerDataEvent evnt)
    {
        evnt.PlayerDataEntity.GetState<IPlayerState>().Name = evnt.Name;
    }

    public void CallReadyToServer()
    {
        if (BoltNetwork.IsServer)
        {
            GlobalGameState.instance.HostStartGame();
            return;
        }
        LoadReadyEvent evnt = LoadReadyEvent.Create(GlobalTargets.OnlyServer);
        evnt.PlayerID = PlayerData.myPlayer.GetState<IPlayerState>().ID;
        evnt.Send();

    }

    public override void OnEvent(LoadReadyEvent evnt)
    {
        GlobalGameState.instance.PlayerInitReady();
        GlobalGameState.instance.SetReady(evnt.PlayerID);
        UpdateHostStartButton();

    }

    public void SetReadyUI(int who, bool ready)
    {
        readyText[who].SetActive(ready);
    }

}

public enum TEAM_ID
{

    //UNSET = -1,

    TEAM_A,
    TEAM_B,

    NUM_TOTAL
}
