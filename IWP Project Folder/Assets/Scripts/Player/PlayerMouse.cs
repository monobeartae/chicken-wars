using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class PlayerMouse : MonoBehaviour
{
    //FOR TESTING
    public static PlayerMouse instance = null;

    public LayerMask surfaceLayerMask;

    private BoltEntity playerEntity;
    private PlayerWeapons playerWeapons;

    void Start()
    {
        instance = this;
        StartCoroutine(AttachToPlayer());
    }

    IEnumerator AttachToPlayer()
    {
        while (PlayerData.myPlayer == null)
        {
            yield return null;
        }

        playerEntity = PlayerData.myPlayer;
        playerWeapons = playerEntity.GetComponent<PlayerWeapons>();
    }

    void Update()
    {
        Vector3 mouseSurfacePos = GetCursorInWorldPos();
        transform.position = mouseSurfacePos;

    }

    public Vector3 GetCursorInWorldPos()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 1;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 rayDir = mouseWorldPos - Camera.main.transform.position;
        rayDir.Normalize();

        RaycastHit hitData;
        if (Physics.Raycast(Camera.main.transform.position, rayDir, out hitData, Mathf.Infinity, surfaceLayerMask))
        {

            return Camera.main.transform.position + rayDir * hitData.distance;
        }

        // Out of Map??
        return transform.position;
    }



}
