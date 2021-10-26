using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCPlayerCarController : MonoBehaviour
{
    float _xInput, _yInput, _steeringAngle;
    public float maxSteeringAngle = 40f, motorForce = 50f;
    public WheelCollider frontLeftWheel, frontRightWheel, backWheels;
    public Transform frontLeftTransform, frontRightTransform, backWheelsTransform;
    Vector3 startPos, startRot;
    public bool bCrashed = false; //!=isAlive
    public float maxAliveHeight = 10f, minAliveHeight = -5f;
    public SnCSessionManager sessionManager;

    private void Awake()
    {
        startPos = transform.position;
        startRot = new Vector3 (transform.rotation.x, transform.rotation.y, transform.rotation.z);
    }

    public void CustomUpdate()
    {
        CheckInput();
        SetSteeringAngle();
        SetAcceleration();
        UpdateWheels();
        CheckCrash();
    }

    void CheckInput() 
    {
        _xInput = Input.GetAxis("Horizontal");
        _yInput = Input.GetAxis("Vertical");
    }

    void SetSteeringAngle() 
    {
        _steeringAngle = maxSteeringAngle * _xInput;
        frontLeftWheel.steerAngle =  frontRightWheel.steerAngle = _steeringAngle;
    }

    void SetAcceleration() 
    {
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = _yInput * motorForce;
    }

    private void UpdateWheels()
    {
        UpdateWheel(frontLeftWheel, frontLeftTransform);
        UpdateWheel(frontRightWheel, frontRightTransform);
        UpdateWheel(backWheels, backWheelsTransform);
    }             

    void UpdateWheel(WheelCollider wheelCollider, Transform wheelTransform) 
    {
        Vector3 pos = wheelTransform.position;
        Quaternion rot = wheelTransform.rotation;

        wheelCollider.GetWorldPose(out pos, out rot);

        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    void CheckCrash() 
    {
        if (transform.position.y > maxAliveHeight || transform.position.y < minAliveHeight)
            CrashCar();
    }

    public void CrashCar() 
    {
        bCrashed = true;
        sessionManager.EnableInput(false);
        SnCSessionManager.bInputEnabled = false;
        sessionManager.UIRef.SetCountdownUI("Crash!");
        sessionManager.UIRef.SetRestartText(true);
    }

    public void Reset()
    {
        transform.position = startPos;
        transform.rotation = new Quaternion(startRot.x, startRot.y, startRot.z, 1f);
        GetComponent<Rigidbody>().useGravity = false;
        bCrashed = false;
    }
}
