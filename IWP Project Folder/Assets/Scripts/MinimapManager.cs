using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{

    public RawImage minimapImage;
    public Transform minimapFocus;
    public MinimapCamera minimapCam;
    
    private bool isMouseHeldOnMinimap = false;

    void Start()
    {

    }

    void Update()
    {

        if (!isMouseHeldOnMinimap && isMouseOverMinimap()
            && Input.GetKeyDown(KeyCode.Mouse0))
        {
            isMouseHeldOnMinimap = true;
            minimapCam.DetachFromPlayer();
        }

        if (isMouseHeldOnMinimap)
        {
            UpdateMinimapFocusPoint();
            UpdateCameraThroughMinimap();

            if (!Input.GetKey(KeyCode.Mouse0))// || !isMouseOverMinimap())
            {
                isMouseHeldOnMinimap = false;
                minimapCam.AttachToPlayer();
                GameSceneManager.instance.SetCameraFocus(PlayerData.myPlayer.transform);
                minimapFocus.transform.position = Vector3.zero;
            }
        }

    }

    private void UpdateMinimapFocusPoint()
    {
        // Get Scale
        float minimap_rad = minimapImage.GetComponent<RectTransform>().rect.height * 0.5f;
        float map_rad = minimapCam.transform.position.y * Mathf.Tan(Mathf.Deg2Rad * 30.0f);
       
        float scale = map_rad / minimap_rad;
        // Get Offset from Center
        Vector3 offset_minimap = Input.mousePosition - minimapImage.transform.position;
        Vector3 offset_map_centre = new Vector3(offset_minimap.x * scale, 0.0f, offset_minimap.y * scale);
        // Set Focus
        Vector3 finalPos = minimapCam.transform.position + offset_map_centre;
        finalPos.y = 0;
        float currx = finalPos.x;
        //float currx = Mathf.Lerp(minimapFocus.position.x, finalPos.x, 0.2f);
        float currz = finalPos.z;
       // float currz = Mathf.Lerp(minimapFocus.position.z, finalPos.z, 0.2f);
        minimapFocus.position = new Vector3(currx, 0.0f, currz);
        
    }
    private void UpdateCameraThroughMinimap()
    {
        GameSceneManager.instance.SetCameraFocus(minimapFocus);
       // minimapCam.SetTargetFocus(minimapFocus.transform.position.x, minimapFocus.transform.position.z);
        // minimapCam.Constraint();
        
    }

    private bool isMouseOverMinimap()
    {
        Vector3 minimapPos = minimapImage.transform.position;
        Vector3 mousePos = Input.mousePosition;
        float width = minimapImage.GetComponent<RectTransform>().rect.width * 0.5f;
        float height = minimapImage.GetComponent<RectTransform>().rect.height * 0.5f;

        return mousePos.x < minimapPos.x + width && mousePos.x > minimapPos.x - width &&
            mousePos.y < minimapPos.y + height && mousePos.y > minimapPos.y - height;
    }


}
