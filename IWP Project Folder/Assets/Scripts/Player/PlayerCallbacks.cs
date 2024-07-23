using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class PlayerCallbacks : GlobalEventListener
{

    public override void OnEvent(StartGameEvent evnt)
    {
        StartCoroutine(PlayerStartGameInits());
    }

    IEnumerator PlayerStartGameInits()
    {
        // Enable Player Weapons
        PlayerWeapons weaponManager = GetComponent<PlayerWeapons>();
        weaponManager.enabled = true;
        weaponManager.Init();

        // Enable Player Lanterns
        PlayerLantern playerLantern = GetComponent<PlayerLantern>();
        playerLantern.enabled = true;
        playerLantern.Init();

        // Wait for Map to Spawn
        while (GameObject.FindWithTag("Map") == null)
            yield return null;

        // Enable HP Bar
        GetComponent<PlayerHP>().ShowHPBar();

        // Teleport players to start point
        BoltEntity player = GetComponent<BoltEntity>();
        IPlayerState playerState = player.GetState<IPlayerState>();

        PlayerController playerController = GetComponent<PlayerController>();
        playerController.SpawnPos = GameSceneManager.spawnPos[playerState.ID];
        switch (playerState.ID)
        {
            case 0:
            case 2:
            case 4:
                playerController.SpawnRot = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                break;
            case 1:
            case 3:
            case 5:
                playerController.SpawnRot = Quaternion.Euler(0.0f, -90.0f, 0.0f);
                break;
        }

        GetComponent<PlayerController>().SetToSpawn();
    }

   
}
