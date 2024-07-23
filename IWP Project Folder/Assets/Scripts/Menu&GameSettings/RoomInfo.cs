using System;
using UnityEngine;
using UnityEngine.UI;
using UdpKit;

public class RoomInfo : MonoBehaviour
{
    public Text roomName;
    public Text membersInfo;
    public Button joinButton;

    void Start()
    {
       // roomName = transform.Find("RoomName").GetComponent<Text>();
       // membersInfo = transform.Find("NumMembers").GetComponent<Text>();
    }

    public void Populate(UdpSession match, Action clickAction)
    {
        roomName.text = match.HostName;
     
        membersInfo.text = string.Format("{0}/{1}", match.ConnectionsCurrent, match.ConnectionsMax);

        if (match.ConnectionsCurrent == match.ConnectionsMax)
        {
            joinButton.gameObject.SetActive(false);
        }
        else
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(clickAction.Invoke);

        }

        //gameObject.GetComponent<Image>().color = backgroundColor;
    }

}
