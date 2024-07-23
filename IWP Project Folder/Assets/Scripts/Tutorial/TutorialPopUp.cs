using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPopUp : MonoBehaviour
{
    public PopUpID ID;
}

public enum PopUpID
{
    MOVEMENT,
    TEAM_BASE,
    FLASHLIGHT,
    MANA_WELL,
    CRYSTAL,
    TURRET,
    SHOP,
    WORKSHOP,
    METALON,
    BREAKABLE,
    WEAPONS,

    NUM_TOTAL
}
