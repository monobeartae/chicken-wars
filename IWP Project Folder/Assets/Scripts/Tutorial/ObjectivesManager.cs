using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectivesManager : MonoBehaviour
{
    //public LineRenderer lineRenderer;
    public VolumetricLines.VolumetricLineStripBehavior lineRenderer;
    private NavMeshAgent playerAgent;
    private const float NAV_PATH_CALCULATE_INTERVAL = 0.3f;
    private float nav_reset_timer = NAV_PATH_CALCULATE_INTERVAL;

    private Objective currObjective;
    private NavMeshPath navPath;
    private GameObject player;
    public delegate void DestinationReached();
    public static event DestinationReached OnDestinationReached;

    void Start()
    {
        StartCoroutine(LinkPlayer());
        navPath = new NavMeshPath();
        currObjective.ID = OBJECTIVE_ID.UNSET;
    }

    IEnumerator LinkPlayer()
    {
        while (PlayerData.myPlayer == null)
            yield return null;

        player = PlayerData.myPlayer.gameObject;

        GameObject go = new GameObject();
        playerAgent = go.AddComponent<NavMeshAgent>();
        playerAgent.radius = 0.2f;
        playerAgent.isStopped = true;
    }

    void Update()
    {
        if (currObjective.ID == OBJECTIVE_ID.UNSET)
            return;

        nav_reset_timer -= Time.deltaTime;
        if (nav_reset_timer <= 0.0f)
        {
            nav_reset_timer = NAV_PATH_CALCULATE_INTERVAL;
            UpdateNavigationUI();
        }

        Vector3 displacement = currObjective.destination - player.transform.position;
        displacement.y = 0;
        float dis = displacement.magnitude;
        if (dis < 1.0f)
        {
            OnDestinationReached?.Invoke();
        }
    }

    void UpdateNavigationUI()
    {
        playerAgent.transform.position = player.transform.position;
        playerAgent.CalculatePath(currObjective.destination, navPath);
        UpdateRenderedPath(navPath.corners);
    }
    void UpdateRenderedPath(Vector3[] path)
    {
        Vector3[] linePath;

        // brain not wokring AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
        // TBC -  un cheat fix this shit piece of code in future
        switch (path.Length)
        {
            case 0:
            case 1:
                return;
            case 2:
                linePath = new Vector3[] { path[0], path[1], path[1] };
                break;
            default:
                linePath = path;
                break;
        }
       

        

        lineRenderer.UpdateLineVertices(linePath);

    }

    public void SetNavigationObjective(OBJECTIVE_ID ID, Vector3 target)
    {
        currObjective.ID = ID;
        currObjective.destination = target;
    }

    public void DisableNavigationObjective()
    {
        this.enabled = false;
    }

    public OBJECTIVE_ID GetCurrentObjectiveID()
    {
        return currObjective.ID;
    }
}

public enum OBJECTIVE_ID
{
    UNSET,

    MID_LANE,
    MANA_WELL,
    METALON,
    SHOP,
    WORKSHOP,

    NUM_TOTAL
}

public struct Objective
{
    public OBJECTIVE_ID ID;
    public Vector3 destination;
}

