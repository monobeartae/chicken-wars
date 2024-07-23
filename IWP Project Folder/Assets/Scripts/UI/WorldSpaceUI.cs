
using UnityEngine;

public class WorldSpaceUI : MonoBehaviour
{
    // script to auto rotate world space canvases to face player
    void Update()
    {
        // Rotate canvas to face player
        // Get Facing Camera Direction
        Vector3 targetDir = Camera.main.transform.forward;
        targetDir.Normalize();

        // Get angle to rotate
        Quaternion m_Rotation = Quaternion.LookRotation(targetDir);
        transform.rotation = m_Rotation;
    }
}