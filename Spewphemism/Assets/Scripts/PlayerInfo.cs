using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class PlayerInfo
{
    PhotonPlayer networkPlayer;
    public Image sprite;
    int roundScore;
    int totalScore;
    int m_id;
    string nickName;

    public int RoundScore
    {
        get { return roundScore; }
        set
        {
            roundScore = value;
            totalScore += roundScore;
        }
    }
    public int TotalScore { get { return totalScore; } set { totalScore = value; } }

    public PlayerInfo(PhotonPlayer player)
	{
        SetPlayer(player);
    }

    public void SetPlayer(PhotonPlayer player)
    {
        networkPlayer = player;
        m_id = player.ID;
        nickName = player.NickName;
    }

    public void ResetScore()
    {
        RoundScore = 0;
        TotalScore = 0;
    }

    public string Name
    {
        get { return nickName; }
    }

    public int Id
    {
        get { return m_id; }
    }
    
    public bool IsInRoom()
    {
        if (networkPlayer != null)
        {
            foreach (var p in PhotonNetwork.playerList)
            {
                if (p.ID == networkPlayer.ID)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
