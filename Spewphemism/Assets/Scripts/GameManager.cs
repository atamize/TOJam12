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

    State m_state = State.Intro;

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
                    SetState(State.Clues);
                }
                break;
        }
    }

    void SetState(State state)
    {
        m_state = state;
        switch (state)
        {
            case State.Clues:

                break;
        }
    }
}

