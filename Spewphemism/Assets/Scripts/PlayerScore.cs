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
    
    public void SetScore(PlayerInfo player, string clue)
    {
        nameLabel.text = player.Name;
        clueLabel.text = clue;
        roundScoreLabel.text = player.RoundScore.ToString();
        totalScoreLabel.text = player.TotalScore.ToString();
    }
}
