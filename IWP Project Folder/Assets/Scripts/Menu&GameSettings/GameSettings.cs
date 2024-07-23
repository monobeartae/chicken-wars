using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSettings
{
    // Player Keybinds
    public static KeyCode INTERACT_KEY = KeyCode.E;
    public static KeyCode RECALL_KEY = KeyCode.R;
    public static KeyCode DROPWEAPON_KEY = KeyCode.Mouse2;
    public static KeyCode ATTACK_KEY = KeyCode.Mouse0;
    public static KeyCode CANCELATTACK_KEY = KeyCode.Mouse1;
    public static KeyCode TOGGLE_LANTERN_KEY = KeyCode.F;
    public static KeyCode TOGGLE_SHOP_KEY = KeyCode.Tab;
    public static KeyCode TOGGLE_WORKSHOP_KEY = KeyCode.Tab;

    // Game Settings  (for my own ref)
    // Coin Award Amount
    //public static int BREAK_BOX_COINS = 100;
    public static int PASSIVE_COINS = 50;
    public static int MINION_KILL_COINS = 20;
    public static int METALON_KILL_COINS = 500;
    // public static int PLAYER_ASSIST_COINS = 100; // LAZY KEEP TRACK OF ATTACKERS; but possible, alr changed framework to take note
    public static int PLAYER_KILL_COINS = 500;
    public static int TURRET_DESTROYED_COINS = 500;

    public delegate void GameEnd(bool won);
    public static event GameEnd OnGameEnd;

    public static void EndGame(bool won)
    {
        OnGameEnd?.Invoke(won);
    }
}
