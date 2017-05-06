using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    const int CLUE_TIME = 60;
    const int GUESS_TIME = 30;

    List<string> IGNORE_WORDS = new List<string>
    {
        "a", "an", "the", "i", "you", "of", "on", "in", "as", "at", "from", "out", "to", "with", "who", "what", "where", "when", "why", "how", "then", "than", "that", "which", "it", "its", "it's", "he", "she", "his", "her", "hers", "those", "them", "i'm", "be", "am", "i'll", "their", "theirs", "they", "they're"
    };

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
    public GameObject guessState;
    public GameObject playerEntries;

    public GameObject timerObject;
    public TextMeshProUGUI timerLabel;
    public TextMeshProUGUI clueInstructionsLabel;

    public TextMeshProUGUI categoryLabel;
    public TextMeshProUGUI clueGiverLabel;
    public TextMeshProUGUI clueLabel;
    public TextMeshProUGUI guesserLabel;
    public TextMeshProUGUI guessLabel;

    public GameObject incorrectObject;
    public GameObject correctObject;

    public PlayerScore[] playerScores;

    List<PlayerInfo> m_playerList;
    List<TargetData> m_targets;
    List<string> m_usedTargets = new List<string>();
    List<Clue> m_clues = new List<Clue>();
    List<string> m_submittedClueWords = new List<string>();

    State m_state = State.Login;
    TargetData m_currentTarget;
    int m_guesserIndex = 0;
    string ogClueInstructions;
    int m_clueIndex = 0;
    bool m_waiting = false;
    Coroutine timerRoutine;

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

            case State.Guess:
                if (eventCode == SpewEventCode.SubmitGuess)
                {
                    SubmitGuess(content);
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
        m_submittedClueWords.Clear();

        do
        {
            int index = Random.Range(0, m_targets.Count);
            m_currentTarget = m_targets[index];
        } while (m_usedTargets.Contains(m_currentTarget.id));

        m_usedTargets.Add(m_currentTarget.id);
        m_currentTarget.SetPattern();

        string guesser = m_playerList[m_guesserIndex].Name;
        string content = m_currentTarget.name + ";" + m_currentTarget.category + ";" + m_currentTarget.FormatDisallowedWords() + ";" + guesser;

        Main.RaiseEvent(SpewEventCode.SendTarget, content);

        if (string.IsNullOrEmpty(ogClueInstructions))
            ogClueInstructions = clueInstructionsLabel.text;

        clueInstructionsLabel.text = string.Format(ogClueInstructions, guesser);

        int time = (m_guesserIndex == 0) ? CLUE_TIME * 2 : CLUE_TIME;
        timerRoutine = StartCoroutine(TimerRoutine(time, () => SetState(State.Guess)));
    }

    IEnumerator TimerRoutine(int seconds, System.Action callback)
    {
        timerObject.SetActive(true);
        
        int sec = seconds;
        for (int i = 0; i < seconds; ++i)
        {
            System.TimeSpan ts = System.TimeSpan.FromSeconds(seconds - i);
            timerLabel.text = string.Format("{0}:{1}", ts.Minutes, ts.Seconds);
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

        string norm = content.ToLower();
        List<string> tokens = new List<string>(norm.Split(' '));
        tokens.RemoveAll(s => IGNORE_WORDS.Contains(s));

        bool alreadyEntered = tokens.Any(t => m_submittedClueWords.Contains(t));

        if (alreadyEntered)
            Debug.Log("Already entered: " + content);

        if (!alreadyEntered && m_currentTarget.IsClueValid(content))
        {
            Clue clue = new Clue();
            clue.player = player;
            clue.clue = content;
            m_clues.Insert(0, clue);

            m_submittedClueWords.AddRange(tokens);

            StringBuilder sb = new StringBuilder();
            foreach (string s in m_submittedClueWords)
            {
                sb.Append(s);
                sb.Append(", ");
            }

            Main.RaiseEvent(SpewEventCode.ValidClue, string.Empty, player.Id);
            Main.RaiseEvent(SpewEventCode.UpdateWords, sb.ToString());

            if (m_state == State.Clues && m_clues.Count == m_playerList.Count - 1)
            {
                if (timerRoutine != null)
                    StopCoroutine(timerRoutine);

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
        guessState.SetActive(true);
        clueState.SetActive(false);
        m_clueIndex = 0;

        // Add players who didn't get to enter a valid clue
        foreach (var p in m_playerList)
        {
            if (p.Id != m_playerList[m_guesserIndex].Id)
            {
                if (!m_clues.Any(c => p.Id == c.player.Id))
                {
                    Clue clue = new Clue();
                    clue.player = p;
                    m_clues.Insert(0, clue);
                }
            }
        }

        categoryLabel.text = m_currentTarget.category;

        NextClue();
    }

    string Possessify(string str)
    {
        if (str.EndsWith("S"))
        {
            return str + "'";
        }

        return str + "'S";
    }

    bool NextClue()
    {
        if (m_clueIndex < m_clues.Count)
        {
            Clue clue = m_clues[m_clueIndex];
            clueGiverLabel.text = string.Format("{0} CLUE:", Possessify(clue.player.Name));
            clueLabel.text = clue.clue.ToUpper();

            guesserLabel.text = string.Format("{0} GUESS:", Possessify(m_playerList[m_guesserIndex].Name));
            guessLabel.text = string.Empty;

            int time = (m_clueIndex == 0 && m_guesserIndex == 0) ? GUESS_TIME * 2 : GUESS_TIME;
            timerRoutine = StartCoroutine(TimerRoutine(GUESS_TIME, IncorrectGuess));
            return true;
        }
        else
        {
            SetState(State.Tally);
        }

        return false;
    }

    void SubmitGuess(string guess)
    {
        if (string.IsNullOrEmpty(guess))
        {
            StartCoroutine(TypeOutGuess("???????"));
        }
        else
        {
            StartCoroutine(TypeOutGuess(guess));
        }
    }

    void IncorrectGuess()
    {
        StartCoroutine(IncorrectRoutine());
    }

    IEnumerator IncorrectRoutine()
    {
        //incorrectObject.transform.localScale = Vector3.zero;
        incorrectObject.SetActive(true);
        //incorrectObject.transform.DOPunchScale(Vector3.one, 0.5f);

        yield return new WaitForSeconds(2f);

        incorrectObject.SetActive(false);
        m_clueIndex++;

        if (NextClue())
        {
            Main.RaiseEvent(SpewEventCode.NextClue, string.Empty, m_playerList[m_guesserIndex].Id);
        }
    }

    IEnumerator TypeOutGuess(string guess)
    {
        StringBuilder sb = new StringBuilder();
        m_waiting = true;
        StopCoroutine(timerRoutine);

        for (int i = 0; i < guess.Length; ++i)
        {
            sb.Append(guess[i]);
            guessLabel.text = sb.ToString();
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(2f);

        if (m_currentTarget.IsGuessValid(guess))
        {
            //correctObject.transform.localScale = Vector3.zero;
            correctObject.SetActive(true);
            //correctObject.transform.DOPunchScale(Vector3.one, 0.5f);

            yield return new WaitForSeconds(2f);

            correctObject.SetActive(false);
            SetState(State.Tally);
        }
        else
        {
            IncorrectGuess();
        }
    }

    void EnterTally()
    {
        int i = 0;
        int correctScore = 0;

        for (i = 0; i < m_playerList.Count; ++i)
        {
            var score = playerScores[i];

            if (i == m_guesserIndex)
            {
                correctScore = 100 * (m_clues.Count - m_clueIndex);
                m_playerList[i].RoundScore = correctScore;
                string clue = (m_clueIndex < m_clues.Count) ? "(Guessed correctly)" : "(FAILED TO GUESS CORRECTLY)";
                score.SetScore(m_playerList[i], clue);
            }
            else
            {
                int roundScore = 0;
                if (m_clueIndex < m_clues.Count)
                {
                    if (m_clues[m_clueIndex].player.Id == m_playerList[i].Id)
                    {
                        roundScore = correctScore;
                    }
                    else
                    {
                        for (int j = m_clueIndex; j < m_clues.Count; ++j)
                        {
                            if (m_clues[j].player.Id == m_playerList[i].Id)
                            {
                                roundScore = 100 * (m_clues.Count - m_clueIndex);
                                break;
                            }
                        }
                    }
                }
                
                var clue = m_clues.Single(c => c.player.Id == m_playerList[i].Id);
                score.SetScore(m_playerList[i], clue.clue);
            }

            score.gameObject.SetActive(true);
        }

        for (; i < playerScores.Length; ++i)
        {
            playerScores[i].gameObject.SetActive(false);
        }
    }
}

