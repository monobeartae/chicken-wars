using System;
using UnityEngine;

using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using UdpKit;

public class ServerSettings : GlobalEventListener
{

    private static string roomID = "";

    void Start()
    {
        LobbyManager.OnClickJoinSession += JoinSession;
    }

    public static void StartAsServer(string room_name)
    {
        if (room_name == "")
            roomID = PlayerData.PlayerName + "'s Room";
        else
            roomID = room_name;


        BoltLauncher.StartServer();
    }

    public static void StartAsClient()
    {
        BoltLauncher.StartClient();
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
            CreateSession();

            // Create Global Game State Object to be used to control game session data
            BoltEntity globalGameState = BoltNetwork.Instantiate(BoltPrefabs.GlobalGameState);
            globalGameState.TakeControl();
        }
    }

    public static void JoinSession(UdpSession session)
    {
        BoltMatchmaking.JoinSession(session.HostName);
    }

    void CreateSession()
    {

        PhotonRoomProperties roomProperties = new PhotonRoomProperties();
    
        roomProperties.IsOpen = true;
        roomProperties.IsVisible = true;

        string sync_scene = "LobbyScene";
        BoltMatchmaking.CreateSession(
            sessionID: roomID, 
            token: roomProperties,
            sceneToLoad: sync_scene
        );

       
    }


}
