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
    const int CLUE_TIME_PER_EXTRA_PLAYER = 10;
    const int GUESS_TIME = 30;
    const int TALLY_TIME = 20;

    List<string> IGNORE_WORDS = new List<string>
    {
        "a", "an", "the", "i", "you", "of", "on", "in", "as", "at", "from", "out", "to", "with", "who", "what", "where", "when", "why", "how", "then", "than", "that", "which", "it", "its", "it's", "he", "she", "his", "her", "hers", "those", "them", "i'm", "be", "am", "i'll", "their", "theirs", "they", "they're"
    };

    public enum State
    {
        Intro,
        Login,
        Clues,
        Guess,
        Tally,
        Final
    }

    class Clue
    {
        public PlayerInfo player;
        public string clue;
        public int booCount;
    }

    public GameObject titleState;
    public GameObject loginState;
    public GameObject clueState;
    public GameObject guessState;
    public GameObject tallyState;
    public GameObject finalState;
    public GameObject playerEntries;

    public GameObject timerObject;
    public TextMeshProUGUI timerLabel;
    public TextMeshProUGUI clueInstructionsLabel;

    public TextMeshProUGUI categoryLabel;
    public TextMeshProUGUI clueGiverLabel;
    public TextMeshProUGUI clueLabel;
    public TextMeshProUGUI guesserLabel;
    public TextMeshProUGUI guessLabel;

    public UnityEngine.UI.Image clueGuesserImage;
    public UnityEngine.UI.Image guesserImage;
    public UnityEngine.UI.Image clueGiverImage;
    public UnityEngine.UI.Image winnerImage;

    public TextMeshProUGUI winnerLabel;

    public GameObject incorrectObject;
    public GameObject correctObject;

    public Transform goat;
    public Transform goatSprite;

    public TextMeshProUGUI targetLabel;
    public PlayerScore[] playerScores;

    List<PlayerInfo> m_playerList;
    List<TargetData> m_targets;
    List<TargetData> m_usedTargets = new List<TargetData>();
    List<Clue> m_clues = new List<Clue>();
    List<string> m_submittedClueWords = new List<string>();

    State m_state = State.Intro;
    TargetData m_currentTarget;
    int m_guesserIndex = 0;
    string ogClueInstructions;
    int m_clueIndex = 0;
    Coroutine timerRoutine;

    void Start()
	{
        AudioManager.Instance.Play("ClueMusic");
	}

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (m_state == State.Tally)
            {
                if (m_guesserIndex < m_playerList.Count - 1)
                {
                    m_guesserIndex++;
                    SetState(State.Clues);
                }
                else
                {
                    SetState(State.Final);
                }
            }
        }
    }
#endif

    public void StartGame(List<PlayerInfo> list, List<TargetData> targets)
    {
        titleState.SetActive(false);
        loginState.SetActive(true);
        m_playerList = list;
        m_targets = targets;
        SetState(State.Login);
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

            case State.Tally:
                if (eventCode == SpewEventCode.BooPlayer)
                {
                    if (!playerScores.Any(p => p.boo.activeInHierarchy))
                    {
                        int index = int.Parse(content);
                        m_clues[index].booCount++;

                        int count = m_clues.Count;
                        if ((count == 3 && m_clues[index].booCount == 2) || (count != 3 && m_clues[index].booCount >= count / 2))
                        {
                            foreach (var score in playerScores)
                            {
                                if (score.ID == m_clues[index].player.Id && !score.boo.activeInHierarchy)
                                {
                                    score.boo.SetActive(true);
                                    m_clues[index].player.TotalScore -= m_clues[index].player.RoundScore;
                                    score.RefreshScore();
                                    AudioManager.Instance.Play("Boo");
                                    break;
                                }
                            }
                        }
                    }
                }
                break;

            case State.Final:
                if (eventCode == SpewEventCode.RestartGame)
                {
                    StopTimer();
                    loginState.SetActive(true);
                    finalState.SetActive(false);

                    foreach (var p in m_playerList)
                    {
                        p.ResetScore();
                    }

                    StartGame();
                }
                else if (eventCode == SpewEventCode.NewPlayers)
                {
                    StopTimer();
                    SetState(State.Intro);
                    m_playerList.Clear();
                }
                break;
        }
    }

    void StartGame()
    {
        if (m_targets.Count == 0)
        {
            m_targets.AddRange(m_targets);
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

    public void SetState(State state)
    {
        if (m_state == state)
            return;

        m_state = state;
        AudioManager.Instance.Play("Vomit");

        switch (state)
        {
            case State.Intro:
                if (PhotonNetwork.connected)
                {
                    PhotonNetwork.Disconnect();
                }
                loginState.SetActive(false);
                clueState.SetActive(false);
                guessState.SetActive(false);
                tallyState.SetActive(false);
                finalState.SetActive(false);
                titleState.SetActive(true);
                goat.gameObject.SetActive(false);

                StopTimer();

                AudioManager.Instance.Play("ClueMusic");
                break;

            case State.Clues:
                EnterClues();
                break;

            case State.Guess:
                EnterGuess();
                break;

            case State.Tally:
                EnterTally();
                break;

            case State.Final:
                EnterFinal();
                break;
        }
    }
    public bool IsInState(State state)
    {
        return m_state == state;
    }

    public void Resume(int playerId)
    {
        switch (m_state)
        {
            case State.Clues:
                Main.RaiseEvent(SpewEventCode.SendTarget, FormClueContent(), playerId);

                StringBuilder sb = new StringBuilder();
                foreach (string s in m_submittedClueWords)
                {
                    sb.Append(s);
                    sb.Append(", ");
                }

                Main.RaiseEvent(SpewEventCode.UpdateWords, sb.ToString(), playerId);
                break;

            case State.Guess:
                if (m_playerList[m_guesserIndex].Id == playerId)
                {
                    Clue clue = m_clues[m_clueIndex];
                    string msg = m_currentTarget.category + ";" + clue.clue.ToUpper();
                    Main.RaiseEvent(SpewEventCode.NextClue, msg, playerId);
                }

                string content = m_playerList[m_guesserIndex].Name;
                Main.RaiseEvent(SpewEventCode.EnterGuess, content, playerId);
                break;

            case State.Tally:
                SendTallyEvent(playerId);
                break;
        }
    }

    string FormClueContent()
    {
        string guesser = m_playerList[m_guesserIndex].Name;
        string content = m_currentTarget.name + ";" + m_currentTarget.category + ";" + m_currentTarget.FormatDisallowedWords() + ";" + guesser + "; " + m_playerList[m_guesserIndex].Id;
        return content;
    }

    void EnterClues()
    {
        StopTimer();
        tallyState.SetActive(false);
        loginState.SetActive(false);
        clueState.SetActive(true);
        m_clues.Clear();
        m_submittedClueWords.Clear();

        int index = Random.Range(0, m_targets.Count);
        m_currentTarget = m_targets[index];
        m_usedTargets.Add(m_currentTarget);
        m_currentTarget.SetPattern();

        m_targets[index] = m_targets.Last();
        m_targets.RemoveAt(m_targets.Count - 1);

        string content = FormClueContent();
        var guesser = m_playerList[m_guesserIndex];
        clueGuesserImage.sprite = guesser.sprite.sprite;

        Main.RaiseEvent(SpewEventCode.SendTarget, content);

        if (string.IsNullOrEmpty(ogClueInstructions))
            ogClueInstructions = clueInstructionsLabel.text;

        clueInstructionsLabel.text = string.Format(ogClueInstructions, guesser.Name);

        int time = CLUE_TIME;
        if (m_guesserIndex == 0)
        {
            time = CLUE_TIME * 2;
        }
        else if (m_playerList.Count > 3)
        {
            time = CLUE_TIME + (CLUE_TIME_PER_EXTRA_PLAYER * (Main.MAX_PLAYERS_IN_ROOM - m_playerList.Count));
        }

        timerRoutine = StartCoroutine(TimerRoutine(time, () => SetState(State.Guess)));

        AudioManager.Instance.Play("ClueMusic");

        DOTween.KillAll();
        goat.gameObject.SetActive(true);
        goat.position = new Vector3(-11f, -2.8f, 0f);
        goatSprite.localPosition = Vector3.zero;
        goat.DOMoveX(11f, 5f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
        goatSprite.DOLocalMoveY(.2f, 0.2f).SetRelative(true).SetLoops(-1, LoopType.Yoyo);
    }

    void StopTimer()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }
    }

    IEnumerator TimerRoutine(int seconds, System.Action callback)
    {
        timerObject.SetActive(true);
        bool canBeep = (m_state == State.Clues || m_state == State.Guess);

        for (int i = 0; i < seconds; ++i)
        {
            System.TimeSpan ts = System.TimeSpan.FromSeconds(seconds - i);
            timerLabel.text = string.Format("{0}:{1:00}", ts.Minutes, ts.Seconds);

            if (ts.Seconds < 11 && canBeep)
            {
                AudioManager.Instance.Play("Beep");
            }
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

        List<string> usedTokens = m_submittedClueWords.Intersect(tokens).ToList();
        bool alreadyEntered = usedTokens.Count > 0;

        StringBuilder sb = new StringBuilder();

        if (alreadyEntered)
        {
            Debug.Log("Already entered: " + content);

            foreach (var s in usedTokens)
            {
                sb.Append(s);
                sb.Append(", ");
            }
            sb.Length -= 2;
            Main.RaiseEvent(SpewEventCode.InvalidClue, sb.ToString(), player.Id);
            return;
        }

        MatchCollection matches = m_currentTarget.ClueMatches(content);

        if (matches.Count == 0)
        {
            Clue clue = new Clue();
            clue.player = player;
            clue.clue = content;
            m_clues.Insert(0, clue);

            m_submittedClueWords.AddRange(tokens);

            foreach (string s in m_submittedClueWords)
            {
                sb.Append(s);
                sb.Append(", ");
            }

            Main.RaiseEvent(SpewEventCode.ValidClue, string.Empty, player.Id);
            Main.RaiseEvent(SpewEventCode.UpdateWords, sb.ToString());

            if (m_state == State.Clues && m_clues.Count == m_playerList.Count - 1)
            {
                StopTimer();
                SetState(State.Guess);
            }
        }
        else
        {
            foreach (var match in matches.Cast<Match>())
            {
                sb.Append(match.Value);
                sb.Append(", ");
            }
            sb.Length -= 2;

            Debug.Log("Sorry, matches: " + sb.ToString());
            Main.RaiseEvent(SpewEventCode.InvalidClue, sb.ToString(), player.Id);
        }
    }

    IEnumerator SendDelayedEvent(float time, System.Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }

    void EnterGuess()
    {
        StopTimer();
        goat.gameObject.SetActive(false);
        guessState.SetActive(true);
        clueState.SetActive(false);
        m_clueIndex = 0;
        guesserImage.sprite = m_playerList[m_guesserIndex].sprite.sprite;

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

        string content = m_playerList[m_guesserIndex].Name;
        Main.RaiseEvent(SpewEventCode.EnterGuess, content);

        AudioManager.Instance.Play("GuessMusic");
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
            clueGiverImage.sprite = clue.player.sprite.sprite;

            if (string.IsNullOrEmpty(clue.clue))
                clue.clue = "??????";

            clueLabel.text = clue.clue.ToUpper();

            guesserLabel.text = string.Format("{0} GUESS:", Possessify(m_playerList[m_guesserIndex].Name));
            guessLabel.text = string.Empty;

            int time = (m_clueIndex == 0 && m_guesserIndex == 0) ? GUESS_TIME * 2 : GUESS_TIME;
            timerRoutine = StartCoroutine(TimerRoutine(time, IncorrectGuess));

            string content = m_currentTarget.category + ";" + clueLabel.text;
            Main.RaiseEvent(SpewEventCode.NextClue, content, m_playerList[m_guesserIndex].Id);
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
        AudioManager.Instance.Play("Wrong");
        incorrectObject.SetActive(true);
        //incorrectObject.transform.DOPunchScale(Vector3.one, 0.5f);

        yield return new WaitForSeconds(2f);

        incorrectObject.SetActive(false);
        m_clueIndex++;

        NextClue();
    }

    IEnumerator TypeOutGuess(string guess)
    {
        StringBuilder sb = new StringBuilder();
        StopTimer();

        for (int i = 0; i < guess.Length; ++i)
        {
            sb.Append(guess[i]);
            guessLabel.text = sb.ToString().ToUpper();
            AudioManager.Instance.Play("Type");
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(2f);

        if (m_currentTarget.IsGuessValid(guess))
        {
            AudioManager.Instance.Play("Yay");
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
        StopTimer();
        tallyState.SetActive(true);
        guessState.SetActive(false);

        targetLabel.text = string.Format("TARGET:\n<b>{0}</b>", m_currentTarget.name.ToUpper());

        int i = 0;

        for (i = 0; i < m_playerList.Count; ++i)
        {
            var score = playerScores[i];

            if (i == m_guesserIndex)
            {
                int correctScore = 200 * (m_clues.Count - m_clueIndex);
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
                        roundScore = 200 * (m_clues.Count - m_clueIndex);
                    }
                    else
                    {
                        for (int j = m_clueIndex; j < m_clues.Count; ++j)
                        {
                            if (m_clues[j].player.Id == m_playerList[i].Id)
                            {
                                roundScore = 100 * (m_clues.Count - j);
                                break;
                            }
                        }
                    }
                }

                m_playerList[i].RoundScore = roundScore;
                var clue = m_clues.Single(c => c.player.Id == m_playerList[i].Id);
                score.SetScore(m_playerList[i], clue.clue);
            }

            score.gameObject.SetActive(true);
        }

        for (; i < playerScores.Length; ++i)
        {
            playerScores[i].gameObject.SetActive(false);
        }

        timerRoutine = StartCoroutine(TimerRoutine(TALLY_TIME, () => {
            if (m_guesserIndex < m_playerList.Count - 1)
            {
                m_guesserIndex++;
                SetState(State.Clues);
            }
            else
            {
                SetState(State.Final);
            }
        }));

        SendTallyEvent();

        AudioManager.Instance.Play("ClueMusic");
    }

    void SendTallyEvent(int playerId = -1)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < m_clues.Count; ++i)
        {
            sb.AppendFormat("{0};", m_clues[i].clue);
        }
        sb.Length--;
        Main.RaiseEvent(SpewEventCode.Tally, sb.ToString(), playerId);
    }

    void EnterFinal()
    {
        StopTimer();
        tallyState.SetActive(false);
        timerObject.SetActive(false);

        var sorted = m_playerList.OrderByDescending(p => p.TotalScore);
        if (sorted.First().TotalScore > sorted.ElementAt(1).TotalScore)
        {
            winnerLabel.text = sorted.First().Name;
            winnerImage.gameObject.SetActive(true);
            winnerImage.sprite = sorted.First().sprite.sprite;
        }
        else
        {
            winnerLabel.text = "YOU";
            winnerImage.gameObject.SetActive(false);
        }

        AudioManager.Instance.Play("Yay");

        finalState.SetActive(true);

        foreach (PlayerInfo player in m_playerList.OrderBy(p => p.Id))
        {
            if (player.IsInRoom())
            {
                Main.RaiseEvent(SpewEventCode.GameOver, string.Empty, player.Id);
                break;
            }
        }

        timerRoutine = StartCoroutine(TimerRoutine(60, () => {
            SetState(State.Intro);
            m_playerList.Clear();
        }
        ));
    }
}

