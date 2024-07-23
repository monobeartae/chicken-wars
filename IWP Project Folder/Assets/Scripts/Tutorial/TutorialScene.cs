using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.AI;
using UnityEngine.UI;
using Photon.Bolt;

using Cinemachine;

public class TutorialScene : GlobalEventListener
{

    public CinemachineVirtualCamera cam;
    public MinimapCamera minimapCam;
    public GameObject[] maps;

    public MinionSpawner minionSpawner;

    public Text UI_TimerText;

    private List<GameObject> TeamAObjectiveList = new List<GameObject>();
    private List<GameObject> TeamBObjectiveList = new List<GameObject>();

    private ObjectivesManager objectivesManager = null;
    private TutorialPopUpsManager popupManager = null;

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
        //StartCoroutine(TutorialSceneInits());
        GameSettings.OnGameEnd += GameEnd;

        objectivesManager = GetComponent<ObjectivesManager>();
        popupManager = GetComponent<TutorialPopUpsManager>();
        ObjectivesManager.OnDestinationReached += OnObjectiveReached;
    }

    // Called when Scene has loaded on Local Machine
    public override void SceneLoadLocalDone(string scene, IProtocolToken token)
    {
        StartCoroutine(TutorialSceneInits());
    }

    IEnumerator TutorialSceneInits()
    {
        // Spawn Player 
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        BoltEntity player = BoltNetwork.Instantiate(BoltPrefabs.Chicken, spawnPos[0], rotation);
        player.TakeControl();
        player.GetState<IPlayerState>().ID = 0;
        player.GetState<IPlayerState>().Team = (int)TEAM_ID.TEAM_A;
        player.GetState<IPlayerState>().TeamSet = true;
        player.GetComponent<PlayerData>().InitPlayerData();
        while (GlobalGameState.instance == null)
        {
            yield return null;
        }
        // Set Map
        GlobalGameState.instance.state.MapID = 0;
        // Skip Player Lobby to Start Game
        GlobalGameState.instance.state.Stage = (int)GAME_STAGE.START_GAME_INIT;
        StartGameEvent evnt = StartGameEvent.Create(GlobalTargets.Everyone);
        evnt.Send();
        GlobalGameState.PauseAllGameUpdates();

      
        // Minimap Camera
        minimapCam.enabled = true;

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
                BoltEntity box = BoltNetwork.Instantiate(id, child.position, child.rotation);
                box.gameObject.GetComponent<NavMeshObstacle>().enabled = false;
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
                BoltEntity fence = BoltNetwork.Instantiate(id, child.position, child.rotation);
                fence.gameObject.GetComponent<NavMeshObstacle>().enabled = false;
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
        // Move Up Game State
        GlobalGameState.instance.state.Stage = (int)GAME_STAGE.START_GAME_TIMER;
        GlobalGameState.instance.StartGameCountdown();


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
        minionSpawner.enabled = true;
        minionSpawner.InitObjectivesList(TeamAObjectiveList, TeamBObjectiveList);

        popupManager.QueuePopUp(PopUpID.MOVEMENT);
        popupManager.QueuePopUp(PopUpID.CRYSTAL);
        popupManager.QueuePopUp(PopUpID.TURRET);
        popupManager.QueuePopUp(PopUpID.FLASHLIGHT);
        SetNextObjective();

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
                UI_TimerText.text = String.Format("{0}:{1}", min, sec);
                // TBC - Give Players coins every interval
                break;
            default:
                break;
        }
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

    // Called Upon Player Leave Game/Game End - TBC!!!!
    public void LeaveCurrentGame()
    {
        BoltNetwork.Shutdown();
        GameStateManager.LoadScene(SCENE_ID.HOMEPAGE);
    }

    void OnObjectiveReached()
    {
        switch (objectivesManager.GetCurrentObjectiveID())
        {
            case OBJECTIVE_ID.UNSET:
                
                break;
            case OBJECTIVE_ID.MID_LANE:
                popupManager.QueuePopUp(PopUpID.BREAKABLE);
                popupManager.QueuePopUp(PopUpID.WEAPONS);
                break;
            case OBJECTIVE_ID.MANA_WELL:
                popupManager.QueuePopUp(PopUpID.MANA_WELL);
                break;
            case OBJECTIVE_ID.METALON:
                break;
            case OBJECTIVE_ID.SHOP:
                break;
            case OBJECTIVE_ID.WORKSHOP:
                break;
            default:
                break;
        }

        SetNextObjective();
    }
    void SetNextObjective()
    {
        switch (objectivesManager.GetCurrentObjectiveID())
        {
            case OBJECTIVE_ID.UNSET:
                objectivesManager.SetNavigationObjective(OBJECTIVE_ID.MID_LANE, new Vector3(0, 0, 0));
                break;
            case OBJECTIVE_ID.MID_LANE:
                objectivesManager.SetNavigationObjective(OBJECTIVE_ID.MANA_WELL, new Vector3(-11.2f, 0, -12.0f));
                break;
            case OBJECTIVE_ID.MANA_WELL:
                break;
            case OBJECTIVE_ID.METALON:
                break;
            case OBJECTIVE_ID.SHOP:
                break;
            case OBJECTIVE_ID.WORKSHOP:
                break;
            default:
                break;
        }
    }
}


enum TutorialPopUpID
{
    NUM_TOTAL
}
