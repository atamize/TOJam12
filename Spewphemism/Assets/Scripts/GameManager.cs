using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    enum State
    {
        Intro,
        Login,
        Clues,
        Guess,
        Tally,
        Final
    }

    List<PlayerInfo> m_playerList;
    List<TargetData> m_targets;
    List<string> m_usedTargets = new List<string>();

    State m_state = State.Login;
    TargetData m_currentTarget;
    int m_guesserIndex = 0;

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
                do
                {
                    int index = Random.Range(0, m_targets.Count);
                    m_currentTarget = m_targets[index];
                } while (m_usedTargets.Contains(m_currentTarget.id));

                string guesser = m_playerList[m_guesserIndex].Name;
                string content = m_currentTarget.name + "," + m_currentTarget.category + "," + guesser;
                Main.RaiseEvent(SpewEventCode.SendTarget, content);
                break;
        }
    }
}

