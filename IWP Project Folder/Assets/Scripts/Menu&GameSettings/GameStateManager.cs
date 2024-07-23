using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Bolt;

public class GameStateManager : MonoBehaviour
{
    public static void LoadScene(SCENE_ID id)
    {
        SceneManager.LoadScene(GetSceneName(id));
    }

    public static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void ExitGame()
    {
        if (BoltNetwork.IsClient)
            BoltNetwork.Shutdown();

        Application.Quit();
    }


    public static SCENE_ID GetCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return GetSceneID(sceneName);
    }
    private static string GetSceneName(SCENE_ID sceneID)
    {
        switch (sceneID)
        {
            case SCENE_ID.MAINMENU:
                return "MainMenuScene";
            case SCENE_ID.HOMEPAGE:
                return "HomePageScene";
            case SCENE_ID.GAMESCENE:
                return "GameScene";
            default:
                return "SampleScene";
        }
    }

    private static SCENE_ID GetSceneID(string scene_name)
    {
        switch (scene_name)
        {
            case "MainMenuScene":
                return SCENE_ID.MAINMENU;
            case "HomePageScene":
                return SCENE_ID.HOMEPAGE;
            case "GameScene":
                return SCENE_ID.GAMESCENE;
            default:
                return SCENE_ID.NUM_TOTAL;
        }
    }
}

public enum SCENE_ID
{
    MAINMENU,
    HOMEPAGE,
    LOBBYSCENE,
    GAMESCENE,

    NUM_TOTAL
}