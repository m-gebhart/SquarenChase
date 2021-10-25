using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCSessionManager : MonoBehaviour
{
    [HideInInspector]
    public static int score = 0, intensity = 0;
    public SnCCamera cameraRef;
    public SnCPlayerCarController carRef;
    public SnCSpawnManager spawnRef;
    public SnCSquareLibrary libraryRef;

    private void FixedUpdate()
    {
        CheckInput();   
    }

    void CheckInput() 
    {
        if (Input.GetKey("r"))
            ResetSession();
    }

    void IncrementIntensity() 
    {
        libraryRef.UpdateCurrentLibrary(++intensity);
    }

    void ResetSession() 
    {
        score = 0;
        intensity = 0;
        cameraRef.Reset();
        carRef.Reset();
        spawnRef.Reset();
        libraryRef.UpdateCurrentLibrary(0);
    }
}
