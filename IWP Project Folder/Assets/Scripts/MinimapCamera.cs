using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{

    GameObject player;
    float height_above_player = 25.0f;

    private bool isFixedToPlayer = true;

    private float radius = 0.0f;
    private Camera cam;

    void Start()
    {
        StartCoroutine(Inits());
    }

    IEnumerator Inits()
    {
        while (PlayerData.myPlayer == null)
            yield return null;

        player = PlayerData.myPlayer.gameObject;

        cam = GetComponent<Camera>();
        radius = transform.position.y * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * 0.5f);
    }

    public float GetCamRadius()
    {
        return radius;
    }

    void FixedUpdate()
    {
        if (isFixedToPlayer)
        {
            transform.position = new Vector3(player.transform.position.x, player.transform.position.y + height_above_player, player.transform.position.z);
            Constraint();
        }
    }
    public void DetachFromPlayer()
    {
        isFixedToPlayer = false;
        Vector3 pos = new Vector3(0.0f, 45.0f, 0.0f);
        transform.position = pos;

        radius = transform.position.y * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * 0.5f);
    }
    public void AttachToPlayer()
    {
        isFixedToPlayer = true;
        Vector3 pos = player.transform.position;
        pos.y = player.transform.position.y + height_above_player;
        transform.position = pos;

        radius = transform.position.y * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * 0.5f);
    }
    public void SetTargetFocus(float posx, float posz)
    {
        transform.position = new Vector3(posx, transform.position.y, posz);
    }

    public void Constraint()
    {
        float map_width = 50.0f;
        float map_height = 30.0f;
        Vector3 currPos = transform.position;
        // limit x
        if (currPos.x - radius < -0.5f * map_width)
            currPos.x = -0.5f * map_width + radius;
        else if (currPos.x + radius > 0.5f * map_width)
            currPos.x = 0.5f * map_width - radius;
        // limit y
        if (currPos.z - radius < -0.5f * map_height)
            currPos.z = -0.5f * map_height + radius;
        else if (currPos.z + radius > 0.5f * map_height)
            currPos.z = 0.5f * map_height - radius;

        transform.position = currPos;
    }
}

struct MinimapIconSettings
{
    public static Color32 AllyIconColour = new Color32(125, 255, 255, 200);
    public static Color32 EnemyIconColour = new Color32(255, 125, 125, 200);
}