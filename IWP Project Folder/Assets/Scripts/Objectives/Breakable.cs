using System;
using UnityEngine;

using Photon.Bolt;

public class Breakable : EntityBehaviour<IBreakableState>
{
    public bool ContainsDrop;

    public override void Attached()
    {
        if (ContainsDrop && entity.IsOwner)
        {
            // Init Randomised Drop
            Array values = WEAPONS.GetValues(typeof(WEAPONS));
            System.Random random = new System.Random();
            // WEAPONS randomDrop = (WEAPONS)values.GetValue(random.Next(values.Length));
            WEAPONS randomDrop = (WEAPONS)UnityEngine.Random.Range(0, (int)WEAPONS.NUM_TOTAL);
            switch (randomDrop)
            {
              
                case WEAPONS.FRYING_PAN:
                    state.Drop = BoltPrefabs.FryingPan;
                    break;
                //case WEAPONS.RAY_GUN:
                //    state.IsEmpty = false;
                //    state.Drop = BoltPrefabs.EggRayGun;
                //    break;
                case WEAPONS.EGG_LAUNCHER:
                    state.Drop = BoltPrefabs.EggLauncher;
                    break;
                default:
                    break;
            }
            
        }
    }


    // Called By Server
    public void Break()
    {
        state.Break();

        // Spawn Weapon
        if (ContainsDrop)
            BoltNetwork.Instantiate(state.Drop, transform.position, Quaternion.identity);

        // Destroy Box
        BoltNetwork.Destroy(gameObject);


    }

  

   
}
