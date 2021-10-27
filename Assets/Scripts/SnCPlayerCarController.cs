using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: DRIFT, right Bounce recognit, rotate on bounce

[System.Serializable]
public struct MotorTorque 
{
    public AnimationCurve torqueCurve;
    public float maxSpeed;
    public float accelerationTime;
}

public class SnCPlayerCarController : MonoBehaviour
{
    float _xInput, _yInput, _steeringAngle, highSpeedThreshold = 5f;
    public MotorTorque motorTorque;
    public float maxSteeringAngle = 40f, bounceBackRange = 0.5f, bounceTime = 0.5f, minBounceLocalZPoint = 1.5f, bounceRotSpeed = 1f;
    public WheelCollider frontLeftWheel, frontRightWheel, backWheels;
    public Transform frontLeftTransform, frontRightTransform, backWheelsTransform;
    Vector3 _startPos, _startRot, _bouncePos, _startBouncePos, _targetBouncePos;
    [HideInInspector]
    public bool bIsBouncing = false, bCrashed = false; //!=isAlive
    public float maxAliveHeight = 10f, minAliveHeight = -5f;
    public SnCSessionManager sessionManager;

    private void Awake()
    {
        _startPos = transform.position;
        _startRot = new Vector3 (transform.rotation.x, transform.rotation.y, transform.rotation.z);
    }

    public void CustomUpdate()
    {
        CheckInput();
        SetSteeringAngle();
        UpdateAcceleration();
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

    float _torqueInputValue = 0f;
    void SetTorqueInputValue(float newValue) 
    {
        _torqueInputValue = newValue;
    }
    void UpdateAcceleration() 
    {
        float torque = 0f;
        if (_yInput != 0f && SnCSessionManager.bInputEnabled)
        {
            _torqueInputValue += Time.deltaTime;
            torque = motorTorque.torqueCurve.Evaluate(_torqueInputValue) * motorTorque.maxSpeed * _yInput;
        }
        else if (_torqueInputValue > 0f)
            _torqueInputValue -= Time.deltaTime;
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = torque;
    }

    private void UpdateWheels()
    {
        if (bIsBouncing)
            UpdateBounceBack();
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pavement"))
            BounceBack(collision);
    }

    void BounceBack(Collision collision) 
    {
        if (IsAtHighSpeed())
        {
            Vector3 contactPoint = collision.GetContact(0).point;
            if (ShouldBounceBack(contactPoint))
                StartBounceBack(collision.GetContact(0));
        }
    }

    bool IsAtHighSpeed() 
    {
        return Mathf.Abs(frontLeftWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold || Mathf.Abs(frontRightWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold;
    }

    bool ShouldBounceBack(Vector3 contactPosition) 
    {
        return Mathf.Abs(this.transform.InverseTransformPoint(contactPosition).z) > minBounceLocalZPoint;
    }

    void StartBounceBack(ContactPoint localContactPoint) 
    {
        bIsBouncing = true;
        _startBouncePos = transform.position;
        _targetBouncePos = transform.position + localContactPoint.normal * bounceBackRange;
        if (localContactPoint.point.z - transform.position.z > 0f) //collision on car's left side
            _bounceRotDir = 1;
        else
            _bounceRotDir = -1;
        _bounceTimer = 0f;
        SetTorqueInputValue(0f);
    }

    float _bounceTimer = 0f;
    int _bounceRotDir = 0;
    void UpdateBounceBack()
    {
        _bounceTimer += Time.fixedDeltaTime;
        _bouncePos = Vector3.Lerp(_startBouncePos, _targetBouncePos , _bounceTimer / bounceTime);
        transform.position = _bouncePos;
        transform.Rotate(new Vector3(0f, bounceRotSpeed*Time.deltaTime*_bounceRotDir, 0f));
        if (_bounceTimer >= bounceTime)
            bIsBouncing = false;
    }

    public Vector3 GetBounceBackPosition() 
    {
        return _bouncePos;
    }

    public void Reset()
    {
        transform.position = _startPos;
        transform.rotation = new Quaternion(_startRot.x, _startRot.y, _startRot.z, 1f);
        GetComponent<Rigidbody>().useGravity = false;
        bCrashed = false;
        bIsBouncing = false;
    }
}
