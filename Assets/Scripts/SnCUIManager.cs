using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnCUIManager : MonoBehaviour
{
    public Text countdownText, scoreText, highScoreText;
    public GameObject restartTextObject;

    public void SetCountdownUI(int countdownValue) 
    {
        countdownText.text = countdownValue.ToString();
    }

    public void SetCountdownUI(float countdownValue) 
    {
        countdownText.text = Mathf.RoundToInt(countdownValue).ToString();
    }

    public void SetCountdownUI(string newCountdownText)
    {
        countdownText.text = newCountdownText;
    }

    public void SetHighScoreUI(int newHighScore) 
    {
        highScoreText.text = "TOP: " + newHighScore.ToString();
    }

    public void SetScoreUI(int newScore) 
    {
        scoreText.text = newScore.ToString();
    }

    public void SetRestartText(bool bEnabled) 
    {
        restartTextObject.SetActive(bEnabled);
    }
}
