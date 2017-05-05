using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlayerEntry : MonoBehaviour
{
    public TextMeshProUGUI nameLabel;
    public Image icon;

    PlayerInfo playerInfo;

    public void SetInfo(PlayerInfo info)
    {
        playerInfo = info;
        nameLabel.text = playerInfo.Name;
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }

    public int Id
    {
        get { return playerInfo.Id; }
    }

    public PlayerInfo Info
    {
        get { return playerInfo; }
    }
}
