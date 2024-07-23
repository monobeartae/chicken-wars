using UnityEngine;

using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using UdpKit;

public class TutorialSettings : GlobalEventListener
{
    private static string roomID = "";

    public static void StartSolo()
    {
        roomID = "Tutorial";

        BoltLauncher.StartServer();
    }


    public override void BoltStartBegin()
    {
        if (BoltNetwork.IsServer)
        {
            BoltNetwork.RegisterTokenClass<PhotonRoomProperties>();
        }
    }

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsServer)
        {
            // Bolt Started As Server
            CreateSession();

            // Create Global Game State Object to be used to control game session data
            BoltEntity globalGameState = BoltNetwork.Instantiate(BoltPrefabs.GlobalGameState);
            globalGameState.TakeControl();
        }
    }

    void CreateSession()
    {
        PhotonRoomProperties roomProperties = new PhotonRoomProperties();

        roomProperties.IsOpen = false;
        roomProperties.IsVisible = false;

        string sync_scene = "TutorialScene";
        BoltMatchmaking.CreateSession(
            sessionID: roomID,
            token: roomProperties,
            sceneToLoad: sync_scene
        );


    }

}
