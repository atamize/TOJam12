﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon;
using System.Text;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.IO;

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
    Tally                = 12,
    UpdateWords          = 13,
    EnterGuess           = 14,
    GameOver             = 15
}


public class Main : PunBehaviour
{
    public string GAME_VERSION = "0.2";
    public string UserId;
    public string previousRoom;
    public string targetsURL = "http://www.baconshark.ca/data/targets.csv";
    public TextAsset targetsCSV;

    public GameObject pauseScreen;
    public GameObject startButton;
    public TextMeshProUGUI connectingLabel;
    public TextMeshProUGUI roomCodeLabel;
    public PlayerEntry[] playerEntries;
    public GameManager gameManager;
    public Button[] yesNoButtons;
    public TextMeshProUGUI versionLabel;

    private const string ALPHA = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const int ROOM_CODE_LENGTH = 4;
    public const int MAX_PLAYERS_IN_ROOM = 6;
    const int MAX_CLUE_CHARACTERS = 20;

    List<PlayerInfo> playerList = new List<PlayerInfo>();
    List<TargetData> targets = new List<TargetData>();
    int pauseButtonSelection = 1;

    void Start()
	{
        ApplyUserIdAndConnect();
        //InitializeTargets(targetsCSV.text);
        StartCoroutine(LoadTargets());
        yesNoButtons[pauseButtonSelection].Select();
    }

    public static string URLAntiCacheRandomizer(string url)
    {
        string r = "";
        r += UnityEngine.Random.Range(
                      1000000, 8000000).ToString();
        r += UnityEngine.Random.Range(
                      1000000, 8000000).ToString();
        string result = url + "?p=" + r;
        return result;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseScreen.activeInHierarchy)
            {
                pauseScreen.SetActive(false);
            }
            else if (!gameManager.IsInState(GameManager.State.Intro))
            {
                pauseScreen.SetActive(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (pauseScreen.activeInHierarchy)
            {
                if (pauseButtonSelection == 1)
                {
                    pauseScreen.SetActive(false);
                }
                else if (pauseButtonSelection == 0)
                {
                    Restart();
                }
            }
            else if (gameManager.IsInState(GameManager.State.Intro))
            {
                if (startButton.activeInHierarchy)
                {
                    OnStartPressed();
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (pauseScreen.activeInHierarchy)
            {
                pauseButtonSelection = (pauseButtonSelection + 1) % 2;
                yesNoButtons[pauseButtonSelection].Select();
            }
        }
    }

    public void Restart()
    {
        playerList.Clear();
        pauseScreen.SetActive(false);
        foreach (PlayerEntry entry in playerEntries)
        {
            entry.gameObject.SetActive(false);
        }
        gameManager.SetState(GameManager.State.Intro);
        connectingLabel.gameObject.SetActive(false);
    }

    public void Unpause()
    {
        pauseScreen.SetActive(false);
    }

    private IEnumerator LoadTargets()
    {
        string path = Application.persistentDataPath + "/targets.csv";
        if (File.Exists(path))
        {
            Debug.Log("Loading targets from: " + path);
            InitializeTargets(File.ReadAllText(path));
            yield break;
        }

        path = Application.persistentDataPath + "/targetsURL.txt";
        string url = targetsURL;

        if (File.Exists(path))
        {
            url = URLAntiCacheRandomizer(File.ReadAllText(path));
        }
        else
        {
            url = URLAntiCacheRandomizer(targetsURL);
        }
        
        Debug.Log("Loading url: " + url);
        WWW w = new WWW(url);
        yield return w;
        if (w.error != null)
        {
            Debug.Log("Error .. " + w.error);
            // for example, often 'Error .. 404 Not Found'
            InitializeTargets(targetsCSV.text);
        }
        else
        {
            Debug.Log("Successfully loaded targets");
            InitializeTargets(w.text);
        }
    }

    void InitializeTargets(string longStringFromFile)
    {
        List<string> lines = new List<string>(
                   longStringFromFile
                   .Split(new string[] { "\r", "\n" },
                   System.StringSplitOptions.RemoveEmptyEntries));

        Debug.Log("Loading targets of length " + lines.Count + ": " + lines[0]);

        string[] header = lines[0].Split(',');
        versionLabel.text = string.Format("Game Version: {0}\nData Version: {1}", GAME_VERSION, header.Last());

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
        //PhotonNetwork.ConnectUsingSettings(GAME_VERSION);

        // this way we can force timeouts by pausing the client (in editor)
        //PhotonHandler.StopFallbackSendAckThread();

        PhotonNetwork.OnEventCall += OnEvent;
    }

    public void OnStartPressed()
    {
        PhotonNetwork.ConnectUsingSettings(GAME_VERSION);

        // this way we can force timeouts by pausing the client (in editor)
        PhotonHandler.StopFallbackSendAckThread();

        AudioManager.Instance.Play("Vomit");

        startButton.SetActive(false);
        connectingLabel.text = "Connecting...";
        connectingLabel.gameObject.SetActive(true);
    }

    public override void OnConnectedToMaster()
    {
        // after connect 
        this.UserId = PhotonNetwork.player.UserId;
        ////Debug.Log("UserID " + this.UserId);


        // after timeout: re-join "old" room (if one is known)
        /*
        if (!string.IsNullOrEmpty(this.previousRoom))
        {
            Debug.Log("ReJoining previous room: " + this.previousRoom);
            PhotonNetwork.ReJoinRoom(this.previousRoom);
            this.previousRoom = null;       // we only will try to re-join once. if this fails, we will get into a random/new room
        }
        else
        */
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
        roomCodeLabel.text = this.previousRoom;

        startButton.SetActive(true);
        connectingLabel.gameObject.SetActive(false);
        foreach (PlayerEntry entry in playerEntries)
        {
            entry.gameObject.SetActive(false);
        }

        gameManager.StartGame(playerList, targets);
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        this.previousRoom = null;
        connectingLabel.text = "Failed to join room. Please try again.";
        startButton.SetActive(true);
    }

    public override void OnConnectionFail(DisconnectCause cause)
    {
        string msg = "Disconnected due to: " + cause + ". this.previousRoom: " + this.previousRoom;
        connectingLabel.text = msg;
        Debug.Log(msg);

        Restart();
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        List<PlayerEntry> inactive = playerEntries.Where(p => !p.gameObject.activeInHierarchy).ToList();
        
        foreach (var e in inactive)
        {
            if (e.Info != null && e.Info.Name == newPlayer.NickName)
            {
                PlayerInfo pInfo = GetPlayerInfoById(e.Info.Id);
                if (pInfo != null)
                {
                    pInfo.SetPlayer(newPlayer);
                }

                e.Info.SetPlayer(newPlayer);
                e.Show(true);

                Debug.Log("Player rejoined: " + newPlayer.NickName + " with id: " + newPlayer.ID);
                gameManager.Resume(newPlayer.ID);
                return;
            }
        }

        PlayerEntry entry = inactive[Random.Range(0, inactive.Count)];
        var info = new PlayerInfo(newPlayer);
        info.sprite = entry.icon;
        entry.SetInfo(info);
        playerList.Add(info);
        entry.Show(true);

        Debug.Log("New player joined: " + newPlayer.NickName);
        AudioManager.Instance.Play("Yay");
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        foreach (var entry in playerEntries)
        {
            if (entry.Info != null && entry.Id == otherPlayer.ID)
            {
                entry.Show(false);
                Debug.Log("Player " + otherPlayer.NickName + " disconnected.");
                break;
            }
        }

        /*
        if (PhotonNetwork.inRoom)
        {
            if (!gameManager.IsInState(GameManager.State.Login))
            {
                Restart();
                connectingLabel.text = otherPlayer.NickName + " disconnected";
            }
        }
        */
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
        else
        {
            options.TargetActors = null;
        }

        Debug.Log("Raise event " + eventCode.ToString() + ": " + content);
        //Debug.Log("Raise event targetActors: " + (options.TargetActors == null? "NULL" : "Has stuff"));
        if (!PhotonNetwork.RaiseEvent((byte)eventCode, content, true, options))
        {
            Debug.LogError("Event could not be sent");
        }
    }

    public void OnSend()
    {
        //string content = "fuckyou,shitbrain";
        //PhotonNetwork.RaiseEvent(1, content, true, RaiseEventOptions.Default);
        //gameManager.StartGame(playerList, targets);
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
