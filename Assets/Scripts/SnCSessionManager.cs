using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCSessionManager : MonoBehaviour
{
    [HideInInspector]
    public static int score = 0, intensity = 0;
    int highScore;
    [HideInInspector]
    public static bool bInputEnabled = false, bAllowAnimCoroutines = true;
    public SnCCamera cameraRef;
    public SnCPlayerCarController carRef;
    public SnCSpawnManager spawnRef;
    public SnCSquareLibrary libraryRef;
    public SnCUIManager UIRef;
    public SnCMaterialManager materialRef;

    private void Start()
    {
        UIRef.SetHighScoreUI(GetHighScore());
    }

    private void FixedUpdate()
    {
        CheckInput();   
    }

    void CheckInput() 
    {
        if (bInputEnabled)
            carRef.CustomUpdate();
        if (Input.GetKey("r"))
            ResetSession();
    }

    void IncrementIntensity() 
    {
        libraryRef.UpdateCurrentLibrary(++intensity);
    }

    public void EnableInput(bool bEnabled) 
    {
        bInputEnabled = bEnabled;
        carRef.GetComponent<Rigidbody>().useGravity = bEnabled;
        carRef.GetComponent<Rigidbody>().constraints = bEnabled ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
    }

    void ResetSession() 
    {
        score = 0;
        intensity = 0;
        bAllowAnimCoroutines = false;
        EnableInput(false);
        cameraRef.Reset();
        carRef.Reset();
        spawnRef.Reset();
        UIRef.SetRestartText(false);
        libraryRef.UpdateCurrentLibrary(0);
        UpdateMaterial();
    }

    void UpdateMaterial() 
    {
        materialRef.ChangeCurrentMaterialSet();
        Camera.main.backgroundColor = materialRef.currentColorMaterialSet.skyBoxColor;
    }

    public void SetScore(int newScore) 
    {
        score = newScore;
        if (score > highScore)
        {
            SetHighScore(score);
            UIRef.SetHighScoreUI(highScore);
        }
        UIRef.SetScoreUI(score);
    }

    void SetHighScore(int newHighScore) 
    {
        highScore = newHighScore;
        SaveHighScore(highScore);
    }

    void SaveHighScore(int newHighScore) 
    {
        PlayerPrefs.SetInt("HighScore", highScore);
    }

    int GetHighScore() 
    {
        return PlayerPrefs.GetInt("HighScore");
    }
}
