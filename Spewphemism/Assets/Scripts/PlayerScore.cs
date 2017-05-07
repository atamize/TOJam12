using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerScore : MonoBehaviour
{
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI clueLabel;
    public TextMeshProUGUI roundScoreLabel;
    public TextMeshProUGUI totalScoreLabel;
    public UnityEngine.UI.Image icon;
    public GameObject boo;

    PlayerInfo info;
    int m_id;
    public int ID {  get { return m_id; } }

    public void SetScore(PlayerInfo player, string clue)
    {
        info = player;
        m_id = player.Id;
        boo.SetActive(false);
        nameLabel.text = player.Name;
        clueLabel.text = clue;
        roundScoreLabel.text = player.RoundScore.ToString();
        totalScoreLabel.text = player.TotalScore.ToString();
        icon.sprite = player.sprite.sprite;
    }

    public void RefreshScore()
    {
        totalScoreLabel.text = info.TotalScore.ToString();
    }
}
