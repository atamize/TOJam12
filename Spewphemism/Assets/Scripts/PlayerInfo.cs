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
        networkPlayer = player;
        m_id = player.ID;

    }

    public void ResetScore()
    {
        RoundScore = 0;
        TotalScore = 0;
    }

    public string Name
    {
        get { return networkPlayer.NickName; }
    }

    public int Id
    {
        get { return m_id; }
    }
}
