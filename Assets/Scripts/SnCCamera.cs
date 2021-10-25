using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCCamera : MonoBehaviour
{
    public float rotateAroundSpeed = 0.1f, lookAtVerticalOffset = 2f, threshold = 1f, dummySpeed = 0.1f;
    public GameObject cameraLookAtDummy;
    [HideInInspector]
    public Vector3 currentSquarePos;
    Vector3 cameraStartPos, dummyStartPos;

    void Start () 
    {
        cameraLookAtDummy.transform.position = GameObject.Find("GameManager").GetComponent<SnCSpawnManager>().GOstartSquare.transform.position + new Vector3(0, lookAtVerticalOffset, 0);
        dummyStartPos = cameraLookAtDummy.transform.position;
        cameraStartPos = transform.position;
    }

    void LateUpdate()
    {
        cameraLookAtDummy.transform.position = Vector3.MoveTowards(cameraLookAtDummy.transform.position, currentSquarePos + new Vector3(0, lookAtVerticalOffset, 0), dummySpeed*Time.deltaTime);
    }

    void FixedUpdate()
    {
        transform.RotateAround(cameraLookAtDummy.transform.position, Vector3.up, rotateAroundSpeed);
        //Look at via Component: Look At Constraint
        //Transfom Pos via child-parent relation
    }

    public void Reset()
    {
        currentSquarePos = Vector3.zero;
        cameraLookAtDummy.transform.position = dummyStartPos;
        transform.position = cameraStartPos;
    }
}
