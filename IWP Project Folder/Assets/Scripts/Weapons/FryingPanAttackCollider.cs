using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class FryingPanAttackCollider : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {

        if (!BoltNetwork.IsServer)
            return;
        if (!other.isTrigger)
            transform.parent.GetComponent<FryingPan>().OnPanCollided(other.gameObject);
    }
}
