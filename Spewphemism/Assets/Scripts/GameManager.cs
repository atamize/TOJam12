using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;

public class GameManager : MonoBehaviour
{
    const int CLUE_TIME = 60;
    const int GUESS_TIME = 30;

    enum State
    {
        Intro,
        Login,
        Clues,
        Guess,
        Tally,
        Final
    }

    struct Clue
    {
        public PlayerInfo player;
        public string clue;
    }

    public GameObject loginState;
    public GameObject clueState;

    public GameObject playerEntries;

    public GameObject timerObject;
    public TextMeshProUGUI timerLabel;
    public TextMeshProUGUI clueInstructionsLabel;


    List<PlayerInfo> m_playerList;
    List<TargetData> m_targets;
    List<string> m_usedTargets = new List<string>();
    List<Clue> m_clues = new List<Clue>();

    State m_state = State.Login;
    TargetData m_currentTarget;
    int m_guesserIndex = 0;
    string ogClueInstructions;

    void Start()
	{

	}

    public void StartGame(List<PlayerInfo> list, List<TargetData> targets)
    {
        m_playerList = list;
        m_targets = targets;
    }

    public void ReceiveEvent(SpewEventCode eventCode, string content, PlayerInfo player)
    {
        switch (m_state)
        {
            case State.Login:
                if (eventCode == SpewEventCode.StartGame)
                {
                    StartGame();
                }
                break;

            case State.Clues:
                if (eventCode == SpewEventCode.SubmitClue)
                {
                    SubmitClue(player, content);
                }
                break;
        }
    }

    void StartGame()
    {
        if (m_usedTargets.Count == m_targets.Count)
        {
            m_usedTargets.Clear();
        }

        m_guesserIndex = 0;

        for (int i = 0; i < m_playerList.Count; ++i)
        {
            int index = Random.Range(0, m_playerList.Count);
            var temp = m_playerList[i];
            m_playerList[i] = m_playerList[index];
            m_playerList[index] = temp;
        }

        SetState(State.Clues);
    }

    void SetState(State state)
    {
        m_state = state;
        switch (state)
        {
            case State.Clues:
                EnterClues();
                break;

            case State.Guess:
                EnterGuess();
                break;
        }
    }

    void EnterClues()
    {
        loginState.SetActive(false);
        clueState.SetActive(true);
        m_clues.Clear();

        do
        {
            int index = Random.Range(0, m_targets.Count);
            m_currentTarget = m_targets[index];
        } while (m_usedTargets.Contains(m_currentTarget.id));

        m_currentTarget.SetPattern();

        string guesser = m_playerList[m_guesserIndex].Name;
        string content = m_currentTarget.name + "," + m_currentTarget.category + "," + guesser;

        Main.RaiseEvent(SpewEventCode.SendTarget, content);

        if (string.IsNullOrEmpty(ogClueInstructions))
            ogClueInstructions = clueInstructionsLabel.text;

        clueInstructionsLabel.text = string.Format(ogClueInstructions, guesser);

        StartCoroutine(TimerRoutine(CLUE_TIME, () => SetState(State.Guess)));
    }

    IEnumerator TimerRoutine(int seconds, System.Action callback)
    {
        timerObject.SetActive(true);
        
        int sec = seconds;
        for (int i = 0; i < seconds; ++i)
        {
            timerLabel.text = (seconds - i).ToString();
            yield return new WaitForSeconds(1);
        }

        timerObject.SetActive(false);
        callback();
    }

    void SubmitClue(PlayerInfo player, string content)
    {
        if (m_clues.Any(c => c.player.Id == player.Id))
        {
            return; // already submitted
        }

        if (m_currentTarget.IsClueValid(content))
        {
            Clue clue = new Clue();
            clue.player = player;
            clue.clue = content;
            m_clues.Add(clue);

            Main.RaiseEvent(SpewEventCode.ValidClue, string.Empty);

            if (m_state == State.Clues && m_clues.Count == m_playerList.Count)
            {
                SetState(State.Guess);
            }
        }
        else
        {
            Main.RaiseEvent(SpewEventCode.InvalidClue, string.Empty);
        }
    }

    void EnterGuess()
    {
        // Add players who didn't get to enter a valid clue
        foreach (var p in m_playerList)
        {
            if (!m_clues.Any(c => p.Id == c.player.Id))
            {
                Clue clue = new Clue();
                clue.player = p;
            }
        }


    }
}

