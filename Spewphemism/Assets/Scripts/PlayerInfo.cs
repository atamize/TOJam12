using UnityEngine;
using System.Collections;

public class PlayerInfo
{
    PhotonPlayer networkPlayer;

	public PlayerInfo(PhotonPlayer player)
	{
        networkPlayer = player;
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
