using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SnCSquareLibrary : MonoBehaviour
{
    [HideInInspector]
    public List<GameObject> ListSquareSelection;

    public List<GameObject> Level1Square;
    public List<GameObject> Level2Square;

    private void Start()
    {
        UpdateCurrentLibrary(0);
    }

    public GameObject GetRandomSquare() 
    {
        int randomNr = Random.Range(0, ListSquareSelection.Count);
        return ListSquareSelection[randomNr];
    }

    public void UpdateCurrentLibrary(int newIntensity) 
    {
        switch (newIntensity)
        {
            case 0:
                ListSquareSelection = Level1Square; break;
            case 1:
                ListSquareSelection = Level2Square; break;
        }
    }
}
