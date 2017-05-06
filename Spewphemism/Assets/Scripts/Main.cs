using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon;
using System.Text;
using UnityEngine.UI;

public enum SpewEventCode
{
    // Incoming events
    JoinRoom             = 0,
    StartGame            = 2,
    SubmitClue           = 3,
    SubmitGuess          = 4,
    BooPlayer            = 5,
    RestartGame          = 6,
    NewPlayers           = 7,

    // Outgoing events
    SendTarget           = 8,
    InvalidClue          = 9,
    ValidClue            = 10,
    NextClue             = 11,
    Tally                = 12
}


public class Main : PunBehaviour
{
    public string UserId;
    public string previousRoom;

    public PlayerEntry[] playerEntries;
    public GameManager gameManager;

    const string GAME_VERSION = "0.1";
    private const string ALPHA = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const int ROOM_CODE_LENGTH = 4;
    const int MAX_PLAYERS_IN_ROOM = 6;
    const int MAX_CLUE_CHARACTERS = 20;

    List<PlayerInfo> playerList = new List<PlayerInfo>();
    List<TargetData> targets = new List<TargetData>();

    void Start()
	{
        ApplyUserIdAndConnect();
        StartCoroutine(LoadTargets());
	}

    private IEnumerator LoadTargets()
    {
        WWW w = new WWW("http://www.baconshark.ca/data/targets.csv");
        yield return w;
        if (w.error != null)
        {
            Debug.Log("Error .. " + w.error);
            // for example, often 'Error .. 404 Not Found'
        }
        else
        {
            Debug.Log("Successfully loaded targets");
            string longStringFromFile = w.text;
            List<string> lines = new List<string>(
                longStringFromFile
                .Split(new string[] { "\r", "\n" },
                System.StringSplitOptions.RemoveEmptyEntries));
            // remove comment lines...
            for (int i = 1; i < lines.Count; ++i)
            {
                string[] split = lines[i].Split(',');
                TargetData data = new TargetData();
                data.id = split[0];
                data.name = split[1];
                data.category = split[2];
                data.responses = split[3];
                data.disallowedWords = split[4];
                data.disallowedRegex = split[5];

                string maxCharacters = split[6];
                data.maxCharacters = string.IsNullOrEmpty(maxCharacters) ? MAX_CLUE_CHARACTERS : int.Parse(maxCharacters);
                targets.Add(data);
            }
        }
    }

    public void ApplyUserIdAndConnect()
    {
        string nickName = "DemoNick";

        if (PhotonNetwork.AuthValues == null)
        {
            PhotonNetwork.AuthValues = new AuthenticationValues();
        }
        //else
        //{
        //    Debug.Log("Re-using AuthValues. UserId: " + PhotonNetwork.AuthValues.UserId);
        //}

        PhotonNetwork.playerName = nickName;
        PhotonNetwork.ConnectUsingSettings(GAME_VERSION);

        // this way we can force timeouts by pausing the client (in editor)
        PhotonHandler.StopFallbackSendAckThread();

        PhotonNetwork.OnEventCall += OnEvent;
    }


    public override void OnConnectedToMaster()
    {
        // after connect 
        this.UserId = PhotonNetwork.player.UserId;
        ////Debug.Log("UserID " + this.UserId);


        // after timeout: re-join "old" room (if one is known)
        if (!string.IsNullOrEmpty(this.previousRoom))
        {
            Debug.Log("ReJoining previous room: " + this.previousRoom);
            PhotonNetwork.ReJoinRoom(this.previousRoom);
            this.previousRoom = null;       // we only will try to re-join once. if this fails, we will get into a random/new room
        }
        else
        {
            StringBuilder sb = new StringBuilder(ROOM_CODE_LENGTH);
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = MAX_PLAYERS_IN_ROOM + 1;

            do
            {
                sb.Length = 0;
                for (int i = 0; i < ROOM_CODE_LENGTH; ++i)
                {
                    sb.Append(ALPHA[Random.Range(0, ALPHA.Length)]);
                }

            } while (!PhotonNetwork.CreateRoom(sb.ToString(), options, null));
        }
    }

    public override void OnJoinedLobby()
    {
        OnConnectedToMaster(); // this way, it does not matter if we join a lobby or not
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = 2, PlayerTtl = 5000 }, null);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.room.Name);
        this.previousRoom = PhotonNetwork.room.Name;

    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        this.previousRoom = null;
    }

    public override void OnConnectionFail(DisconnectCause cause)
    {
        Debug.Log("Disconnected due to: " + cause + ". this.previousRoom: " + this.previousRoom);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        var info = new PlayerInfo(newPlayer);
        playerEntries[playerList.Count].SetInfo(info);
        playerEntries[playerList.Count].Show(true);
        playerList.Add(info);
        
        Debug.Log("New player joined: " + newPlayer.NickName);
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        foreach (var entry in playerEntries)
        {
            if (entry.Id == otherPlayer.ID)
            {
                entry.Show(false);
                playerList.Remove(entry.Info);
                Debug.Log("Player " + otherPlayer.NickName + " disconnected.");
                break;
            }
        }
    }

    PlayerInfo GetPlayerInfoById(int id)
    {
        foreach (var player in playerList)
        {
            if (player.Id == id)
            {
                return player;
            }
        }
        return null;
    }

    void OnEvent(byte eventCode, object content, int senderId)
    {
        Debug.Log("Event code: " + eventCode + ", senderId: " + senderId);
        Debug.Log("content: " + content);

#if UNITY_EDITOR
        if (eventCode == (byte)SpewEventCode.JoinRoom)
        {
            PhotonPlayer p = new PhotonPlayer(false, senderId, content.ToString());
            var info = new PlayerInfo(p);
            playerEntries[playerList.Count].SetInfo(info);
            playerEntries[playerList.Count].Show(true);
            playerList.Add(info);

            Debug.Log("New player joined: " + p.NickName);
            return;
        }
#endif
        PlayerInfo player = GetPlayerInfoById(senderId);
        
        if (eventCode == (byte)SpewEventCode.StartGame)
        {
            gameManager.StartGame(playerList, targets);
        }

        gameManager.ReceiveEvent((SpewEventCode)eventCode, (content == null) ? string.Empty : content.ToString(), player);

        //Hashtable hashTable = content as Hashtable;
        //string msg = hashTable["message"] as string;
    }

    public static void RaiseEvent(SpewEventCode eventCode, string content, int receiverId = -1)
    {
        var options = RaiseEventOptions.Default;

        if (receiverId != -1)
        {
            options.TargetActors = new int[] { receiverId };
        }

        Debug.Log("Raise event " + eventCode.ToString() + ": " + content);
        PhotonNetwork.RaiseEvent((byte)eventCode, content, true, options);
    }

    public void OnSend()
    {
        //string content = "fuckyou,shitbrain";
        //PhotonNetwork.RaiseEvent(1, content, true, RaiseEventOptions.Default);
        gameManager.StartGame(playerList, targets);
    }

#if UNITY_EDITOR
    public void DebugEvent(SpewEventCode eventCode, string content, int senderId)
    {
        if (eventCode == SpewEventCode.JoinRoom)
        {
            OnPhotonPlayerConnected(new PhotonPlayer(false, senderId, string.IsNullOrEmpty(content) ? "Player " + senderId : content));
        }
        else
        {
            OnEvent((byte)eventCode, content, senderId);
        }
    }

    public void QuickStart()
    {
        OnPhotonPlayerConnected(new PhotonPlayer(false, 1, "PLAYER1"));
        OnPhotonPlayerConnected(new PhotonPlayer(false, 2, "PLAYER2"));
        OnPhotonPlayerConnected(new PhotonPlayer(false, 3, "PLAYER3"));
        OnEvent((byte)SpewEventCode.StartGame, "", 1);
    }
#endif
}
