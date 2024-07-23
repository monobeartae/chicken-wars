using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

using Cinemachine;


public class GameSceneManager : GlobalEventListener
{
    #region Singleton
    public static GameSceneManager instance = null;
    #endregion



    public Text UI_TimerText;

    public GameObject[] maps;
    public CinemachineVirtualCamera cam;

    private GAME_MODE gameMode = GAME_MODE.FREE_THE_CHICKEN;

    private List<GameObject> TeamAObjectiveList = new List<GameObject>();
    private List<GameObject> TeamBObjectiveList = new List<GameObject>();

    public static Vector3[] spawnPos = new Vector3[6]
    {
        new Vector3(-22.5f, 0.1f, 0.0f),
        new Vector3(22.5f, 0.1f, 0.0f),
        new Vector3(-22.9f, 0.1f, -0.4f),
        new Vector3(22.9f, 0.1f, 0.4f),
        new Vector3(-22.9f, 0.1f, -0.4f),
        new Vector3(22.9f, 0.1f, 0.4f)
    };
    

    void Start()
    {
        instance = this;

        GameSettings.OnGameEnd += GameEnd;
    }


    // Called when Scene has loaded on Local Machine
    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        StartCoroutine(StartGameInits());
    }


    // Local Inits for Every Player 
    IEnumerator StartGameInits()
    {
        while (GlobalGameState.instance == null)
        {
            yield return null;
        }

        // Load Map
        GameObject selected_map = Instantiate(maps[GlobalGameState.instance.state.MapID], Vector3.zero, Quaternion.identity);
        // Instantiate Bolt Entities
        // OBJECTIVES
        Transform objectives = selected_map.transform.Find("Objectives");
        // Main Crystals
        Transform crystals = objectives.Find("Crystals");
        int i = 0;
        foreach (Transform child in crystals)
        {
            if (BoltNetwork.IsServer)
            {
                PrefabId id = child.GetComponent<BoltEntity>().PrefabId;
                BoltEntity crystal = BoltNetwork.Instantiate(id, child.position, child.rotation);
                crystal.GetState<IEggCrystalState>().Team = i;
                if (i == 0)
                    TeamAObjectiveList.Add(crystal.gameObject);
                else
                    TeamBObjectiveList.Add(crystal.gameObject);
            }
            Destroy(child.gameObject);
            i++;
        }
        // Turrets
        Transform turrets = objectives.Find("Turrets");
        foreach (Transform child in turrets)
        {
            if (BoltNetwork.IsServer)
            {
                int TurretTeamID = child.GetComponent<Turret>().TeamID;
                PrefabId id = child.GetComponent<BoltEntity>().PrefabId;
                BoltEntity turretEntity = BoltNetwork.Instantiate(id, child.position, child.rotation);
                Turret turret = turretEntity.gameObject.GetComponent<Turret>();
                turretEntity.GetState<IEntityState>().Team = TurretTeamID;
                turret.state.IsTeamSet = true;
                if (TurretTeamID == 0)
                    TeamAObjectiveList.Add(turret.gameObject);
                else
                    TeamBObjectiveList.Add(turret.gameObject);
            }
            Destroy(child.gameObject);
        }
        // PROPS
        Transform props = selected_map.transform.Find("Props");
        // Boxes
        Transform boxes = props.Find("Boxes");
        foreach (Transform child in boxes)
        {
            if (BoltNetwork.IsServer)
            {
                PrefabId id = child.GetComponent<BoltEntity>().PrefabId;
                BoltNetwork.Instantiate(id, child.position, child.rotation);
            }
            Destroy(child.gameObject);
        }
        // Fences
        Transform fences = props.Find("Fences");
        foreach (Transform child in fences)
        {
            if (BoltNetwork.IsServer)
            {
                PrefabId id = child.GetComponent<BoltEntity>().PrefabId;
                BoltNetwork.Instantiate(id, child.position, child.rotation);
            }
            Destroy(child.gameObject);
        }
        // ENTITIES
        // Metalons
        Transform entities = selected_map.transform.Find("Entities");
        Transform metalons = entities.transform.Find("Metalons");
        foreach (Transform child in metalons)
        {
            Metalon met = child.gameObject.GetComponent<Metalon>();
            met.SpawnAndAttach();
        }

        while (PlayerData.myPlayer == null)
        {
            yield return null;
        }


        // Attach Camera to Player
        Transform playerFocus = PlayerData.myPlayer.transform.Find("FocusPoint");
        SetCameraFocus(playerFocus);
        // Tell Server Local Player Has finihsed main Inits
        CallReadyToServer();


        while (UIManager.instance == null)
            yield return null;

        // Set UI 
        UIManager.instance.SetLoadingScreen(false);
        UI_TimerText.gameObject.SetActive(true);

        // Game Start timer
        while (GlobalGameState.gameStage != GAME_STAGE.IN_GAME)
        {
            UI_TimerText.text = ((int)GlobalGameState.instance.state.Timer).ToString();
            yield return null;
        }
        UI_TimerText.text = "Game Started!";
        // Unlock Player Movement
        GlobalGameState.ResumeAllGameUpdates();
        // Enable Minion Spawner
        MinionSpawner minionSpawner = GetComponent<MinionSpawner>();
        minionSpawner.enabled = true;
        minionSpawner.InitObjectivesList(TeamAObjectiveList, TeamBObjectiveList);




    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            UIManager.instance.ToggleESCMenu();

        switch (GlobalGameState.gameStage)
        {
            case GAME_STAGE.IN_GAME:
                // Update Timer
                int totalTime = (int)GlobalGameState.instance.state.Timer;
                int min = totalTime / 60;
                int sec = totalTime % 60;
                UI_TimerText.text = String.Format("{0}:{1}", min.ToString("D2"), sec.ToString("D2"));
                // TBC - Give Players coins every interval
                break;
            default:
                break;
        }

    }

    // Called Upon Player Leave Game/Game End - TBC!!!!
    public void LeaveCurrentGame()
    {
        BoltNetwork.Shutdown();
        GameStateManager.LoadScene(SCENE_ID.HOMEPAGE);
    }

    void CallReadyToServer()
    {
        LoadReadyEvent evnt = LoadReadyEvent.Create(GlobalTargets.OnlyServer);
        evnt.Send();

    }

    public override void OnEvent(LoadReadyEvent evnt)
    {
        GlobalGameState.instance.PlayerInitReady();
    }

    public void SetCameraFocus(Transform target)
    {
        cam.LookAt = target;
        cam.Follow = target;
    }

    public void GameEnd(bool won)
    {
        UI_TimerText.gameObject.SetActive(false);
        if (won)
            UIManager.instance.ShowVictoryScreen();
        else
            UIManager.instance.ShowDefeatScreen();
    }


}
public enum GAME_MODE
{
    FREE_THE_CHICKEN,
    DEATHMATCH,

    NUM_TOTAL
}

public enum GAME_MAPS
{
    GRASS
}
