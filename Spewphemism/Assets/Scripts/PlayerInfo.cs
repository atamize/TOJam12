using UnityEngine;
using System.Collections;

public class PlayerInfo
{
    PhotonPlayer networkPlayer;
    int roundScore;
    int totalScore;

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
        get { return networkPlayer.ID; }
    }
}
