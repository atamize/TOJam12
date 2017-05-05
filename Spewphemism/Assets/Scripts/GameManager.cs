using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    List<PlayerInfo> playerList;

    void Start()
	{

	}

    public void StartGame(List<PlayerInfo> list)
    {
        playerList = list;
    }

    public void ReceiveEvent(SpewEventCode eventCode, string content, PlayerInfo player)
    {

    }
}

