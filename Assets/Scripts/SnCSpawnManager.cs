using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCSpawnManager : MonoBehaviour
{
    public float squareWidth = 10f, interval = 1f, startCountdown = 3f;
    public GameObject GOstartSquare, scoreTriggerPrefab;
    BoxCollider[] startSquareColliders; 
    GameObject _GOcurrentSquare, _GOnextSquare, _GOpreviousSquare;
    SnCSquare _currentSquare, _nextSquare, _previousSquare;
    Vector3 _newSquarePos, _startSquarePos;

    void Start()
    {
        startSquareColliders = GOstartSquare.GetComponents<BoxCollider>();
        _startSquarePos = GOstartSquare.transform.position;
        GOstartSquare.GetComponent<SnCSquare>().entrySide = 3; //no new square behind car at beginning
        SetSquare(GOstartSquare);
        StartCoroutine("CoroutineSpawnSquares");
    }

    void SetSquare(GameObject nextSquare) 
    {
        if (_GOpreviousSquare != null && _GOpreviousSquare != GOstartSquare)
            _previousSquare.AnimateRemoval(true);
        if (GOstartSquare == _GOpreviousSquare)
            SetStartSquare(false);

        if (_GOcurrentSquare != null)
        {
            _GOpreviousSquare = _GOcurrentSquare;
            _previousSquare = _GOcurrentSquare.GetComponent<SnCSquare>();
        }

        _GOcurrentSquare = nextSquare;
        _currentSquare = _GOcurrentSquare.GetComponent<SnCSquare>();
        Camera.main.GetComponent<SnCCamera>().currentSquarePos = _newSquarePos;
    }

    IEnumerator CoroutineSpawnSquares() 
    {
        yield return new WaitForSeconds(startCountdown);
        while (true)
        {
            yield return new WaitForSeconds(interval);
            SpawnSquare();
        }
    }

    void SpawnSquare() 
    {
        int randomSide = _currentSquare.GetAnyFreeSide();
        _newSquarePos = GetNewSpawnPos(randomSide);

        _GOnextSquare = Instantiate(GetComponent<SnCSquareLibrary>().GetRandomSquare());
        _nextSquare = _GOnextSquare.GetComponent<SnCSquare>();
        _GOnextSquare.transform.position = _newSquarePos + new Vector3(0f, _nextSquare.creationHeight, 0f);
        CheckNextVerticalOffset();
        _nextSquare.entrySide = SnCStaticLibrary.GetNextEntrySideToCurrentExitSideValue(randomSide);
        while (!_nextSquare.IsSideFree(_nextSquare.entrySide)) 
        {
            _nextSquare.Rotate(1);
        }
        SetSquare(_GOnextSquare);
        _nextSquare.AnimateCreation();
    }

    Vector3 GetNewSpawnPos(int side) 
    {
        _currentSquare.exitSide = side;
        switch (side)
        {
            //Spawn Left of Current
            case 0: default:
                return _currentSquare.transform.position - new Vector3(squareWidth, 0, 0);
            //Spawn Top of Current
            case 1:
                return _currentSquare.transform.position + new Vector3(0, 0, squareWidth);
            //Spawn Right of Current
            case 2:
                return _currentSquare.transform.position + new Vector3(squareWidth, 0, 0);
            //Spawn Bottom of Current
            case 3:
                return _currentSquare.transform.position - new Vector3(0, 0, squareWidth);
        }
    }

   void CheckNextVerticalOffset() 
   {
        //for transitioning to and from overpass square
        if (_nextSquare.verticalOffset != 0f && _currentSquare.verticalOffset == 0f)
            _GOnextSquare.transform.position += new Vector3(0f, _nextSquare.verticalOffset, 0f);
        else if (_nextSquare.verticalOffset == 0f && _currentSquare.verticalOffset != 0f) 
            _GOnextSquare.transform.position += new Vector3(0f, -_currentSquare.verticalOffset, 0f);
    }

    void SetStartSquare(bool bEnabled) 
    {
        if (!bEnabled)
            GOstartSquare.GetComponent<SnCSquare>().AnimateRemoval(false);
        else
        {
            GOstartSquare.GetComponent<SnCSquare>().Reset();
            GOstartSquare.transform.position = _startSquarePos;
        }
    }

    public void Reset()
    {
        StopAllCoroutines();
        if (_GOcurrentSquare != GOstartSquare)
            Destroy(_GOcurrentSquare);
        if (_GOpreviousSquare != GOstartSquare)
            Destroy(_GOpreviousSquare);


        _GOpreviousSquare = null;
        _GOnextSquare = null;
        _GOcurrentSquare = GOstartSquare;

        SetStartSquare(true);
        _currentSquare = GOstartSquare.GetComponent<SnCSquare>();
        StartCoroutine("CoroutineSpawnSquares");
    }
}
