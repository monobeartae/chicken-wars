using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class TeamBase : MonoBehaviour
{
    public int TeamID = 0;

    private float heal_rate = 10.0f;
    private float damage_rate = 10.0f;

    private void OnTriggerStay(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;

        if (other.gameObject.CompareTag("Player") && !other.isTrigger)
        {
            PlayerHP playerHP = other.gameObject.GetComponent<PlayerHP>();
            bool friendly = TeamID == playerHP.state.Team;
            if (friendly)
                playerHP.UpdateHP(heal_rate * Time.deltaTime);
            else
                playerHP.UpdateHP(-1 * damage_rate * Time.deltaTime);
        }
    }
}
