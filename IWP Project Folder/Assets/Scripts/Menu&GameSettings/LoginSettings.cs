using System;
using UnityEngine;
using UnityEngine.UI;

public class LoginSettings : Photon.Bolt.GlobalEventListener
{

    public InputField input_displayName;
    public Text display_text;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // Called when Enter Button is Pressed
    public void LoginUser()
    {
        // Validation Checks...
        if (input_displayName.text == "")
        {
            display_text.text = "Display Name cannot be empty! :(";
            return;
        }

        // Set Relevant Local Data
        PlayerData.PlayerName = input_displayName.text;
        // Load Scene
        GameStateManager.LoadScene(SCENE_ID.HOMEPAGE);
    }


}

