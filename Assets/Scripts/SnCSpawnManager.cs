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
    SnCSessionManager _sessionManager;
    Vector3 _newSquarePos, _startSquarePos;
    bool bOnStartSquare = true;

    void Start()
    {
        _sessionManager = GetComponent<SnCSessionManager>(); //attached to same GameObject
        _sessionManager.materialRef.ChangeCurrentMaterialSet();

        //cache start square values
        startSquareColliders = GOstartSquare.GetComponents<BoxCollider>();
        _startSquarePos = GOstartSquare.transform.position;

        SetSquare(GOstartSquare);
        StartCoroutine("CoroutineSpawnSquares");
    }

    void SetStartSquare(bool bEnabled)
    {
        bOnStartSquare = bEnabled;
        if (!bEnabled)
            GOstartSquare.GetComponent<SnCSquare>().AnimateRemoval(false);
        else
        {
            GOstartSquare.GetComponent<SnCSquare>().exitSide = 2; //leaving at right side
            GOstartSquare.transform.position = _startSquarePos;
            GOstartSquare.GetComponent<SnCSquare>().Reset();
        }
    }

    void SetSquare(GameObject nextSquare)
    {
        //remove if not start square
        if (_GOpreviousSquare != null && _GOpreviousSquare != GOstartSquare)
            _previousSquare.AnimateRemoval(true);
        if (GOstartSquare == _GOpreviousSquare)
            SetStartSquare(false);

        //check if beginning square
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
        //Countdown
        for (int countdown = 3; countdown > 0; countdown--)
        {
            _sessionManager.UIRef.SetCountdownUI(countdown);
            yield return new WaitForSeconds(startCountdown/3);
        }
        _sessionManager.EnableInput(true);
        _sessionManager.UIRef.SetCountdownUI("", Color.white);
        SnCSessionManager.bAllowAnimCoroutines = true;

        #if UNITY_EDITOR
        if (_sessionManager.bEditorStaticPlayground)
            yield break;
        #endif
        //Spawn Iteration
        while (!_sessionManager.carRef.bCrashed)
        {
            yield return new WaitForSeconds(interval);
            SpawnSquare();
        }
    }

    void SpawnSquare() 
    {
        int randomExitSide = _currentSquare.exitSide == -1 ? _currentSquare.GetAnyFreeSide() : _currentSquare.exitSide;
        _newSquarePos = GetNewSpawnPos(randomExitSide);

        _GOnextSquare = Instantiate(GetComponent<SnCSquareLibrary>().GetRandomSquare());
        _nextSquare = _GOnextSquare.GetComponent<SnCSquare>();
        _nextSquare.ChangeMaterial(_sessionManager.materialRef.currentMaterials);
        _GOnextSquare.transform.position = _newSquarePos + new Vector3(0f, _nextSquare.creationHeight, 0f);
        CheckNextVerticalOffset();
        _nextSquare.entrySide = SnCStaticLibrary.GetNextEntrySideToCurrentExitSideValue(randomExitSide);
        while (!_nextSquare.IsSideFree(_nextSquare.entrySide)) 
        {
            _nextSquare.Rotate(1);
        }
        SetSquare(_GOnextSquare);
        _nextSquare.SpawnCoins(scoreTriggerPrefab, _sessionManager);
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

    public void Reset()
    {
        SetStartSquare(true);

        if (_GOcurrentSquare != GOstartSquare)
            Destroy(_GOcurrentSquare);
        if (_GOpreviousSquare != GOstartSquare)
            Destroy(_GOpreviousSquare);

        StopEveryCoroutine();
        _GOpreviousSquare = null;
        _GOnextSquare = null;
        _GOcurrentSquare = GOstartSquare;

        _currentSquare = GOstartSquare.GetComponent<SnCSquare>();
        StartCoroutine("CoroutineSpawnSquares");
    }

    void StopEveryCoroutine() 
    {
        StopAllCoroutines();
        SnCSessionManager.bAllowAnimCoroutines = false;
    }
}
