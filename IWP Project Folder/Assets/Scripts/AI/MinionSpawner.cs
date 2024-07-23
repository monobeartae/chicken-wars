using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class MinionSpawner : MonoBehaviour
{
    private float SPAWN_INTERVAL = 30.0f; // ori was 60
    private int MINION_COUNT = 4;
    private float MINION_INTERVAL = 1.5f;
    private float timer = 1.0f; // init to 10

    private Vector3[] minionSpawnPos = new Vector3[2]
    {
        new Vector3(-23.0f, 0.0f, -2.0f),
        new Vector3(23.0f, 0.0f, -2.0f)
    };

    private List<GameObject> TeamAObjectiveList = new List<GameObject>();
    private List<GameObject> TeamBObjectiveList = new List<GameObject>();

    void Start()
    {
        
    }

    public void InitObjectivesList(List<GameObject> teamAList, List<GameObject> teamBList)
    {
        TeamAObjectiveList = teamAList;
        TeamBObjectiveList = teamBList;
    }

    void Update()
    {
        if (!BoltNetwork.IsServer)
            return;

        if (timer > 0.0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0.0f)
            {
                StartCoroutine(SpawnMinions());
                timer = SPAWN_INTERVAL;
            }
        }


    }

    IEnumerator SpawnMinions()
    {
        for (int i = 0; i < MINION_COUNT; i ++)
        {
            Spawn();
            yield return new WaitForSeconds(MINION_INTERVAL);
        }
    }

    void Spawn()
    {
        // Spawn Team A Lane Minions
        BoltEntity minionEntity = BoltNetwork.Instantiate(BoltPrefabs.Minion, minionSpawnPos[0], Quaternion.identity);
        LaneMinion minion = minionEntity.gameObject.GetComponent<LaneMinion>();
        minion.Init(0, TeamBObjectiveList);
        // Spawn Team B Lane Minions
        minionEntity = BoltNetwork.Instantiate(BoltPrefabs.Minion, minionSpawnPos[1], Quaternion.identity);
        minion = minionEntity.gameObject.GetComponent<LaneMinion>();
        minion.Init(1, TeamAObjectiveList);
    }
}
