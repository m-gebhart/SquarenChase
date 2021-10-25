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

    private void Awake()
    {
        startPos = transform.position;
        startRot = new Vector3 (transform.rotation.x, transform.rotation.y, transform.rotation.z);
    }

    private void FixedUpdate()
    {
        CheckInput();
        SetSteeringAngle();
        SetAcceleration();
        UpdateWheels();
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

    public void Reset()
    {
        transform.position = startPos;
        transform.eulerAngles = startRot;
    }
}
