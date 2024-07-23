using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt.Matchmaking;
using UdpKit;
using Photon.Bolt;
using System;

public class LobbyManager : GlobalEventListener
{

    #region Singleton
    public static LobbyManager instance;
    #endregion

    //public GameObject startMatchPanel, matchMakingPanel;
    public GameObject entryPanel, roomListPanel;
    public Text playerInfo_text;
    public Text display_text;

    public InputField createRoomNameIF;

    public RectTransform roomListRect;
    public GameObject roomInfoSlot;

    public static event Action<UdpSession> OnClickJoinSession;

    void Start()
    {
        // UI Inits
        entryPanel.SetActive(true);
        roomListPanel.SetActive(false);
        display_text.gameObject.SetActive(false);

   
        playerInfo_text.text = "Name: " + PlayerData.PlayerName;

    }

  
    void Update()
    {
       
    }

    public void CreateRoom()
    {
        entryPanel.SetActive(false);
        display_text.gameObject.SetActive(true);
        display_text.text = "Creating Room...";
        ServerSettings.StartAsServer(createRoomNameIF.text);
    }
    public void BrowseRoomList()
    {
        entryPanel.SetActive(false);
        display_text.gameObject.SetActive(true);
        display_text.text = "Loading Room List...";
        ServerSettings.StartAsClient();
    }

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsClient)
        {
            display_text.gameObject.SetActive(false);
            roomListPanel.SetActive(true);
        }
    }


    public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
    {
        ClearRoomList();
      
        foreach (var pair in sessionList)
        {
            var session = pair.Value;
            var roomEntryGo = Instantiate(roomInfoSlot, roomListRect, false);

            roomEntryGo.GetComponent<RoomInfo>().Populate(session, () =>
            {
                if (OnClickJoinSession != null) OnClickJoinSession.Invoke(session);
            }); 

        }
    }
    void ClearRoomList()
    {
        foreach (Transform child in roomListRect)
        {
            Destroy(child.gameObject);
        }
    }
   
}
