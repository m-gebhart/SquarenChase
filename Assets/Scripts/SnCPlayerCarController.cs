using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: DRIFT, right Bounce recognit, rotate on bounce

[System.Serializable]
public struct MotorTorque 
{
    public AnimationCurve torqueCurve;
    public float maxSpeed;
    public float brakeTorque;
}

public class SnCPlayerCarController : MonoBehaviour
{
    float _xInput, _yInput, _steeringAngle;
    public MotorTorque motorTorque;
    public float maxSteeringAngle = 40f, driftSpeed = 20f, preDriftTime = 1f, 
        bounceBackRange = 0.5f, bounceTime = 0.5f, bounceRotSpeed = 1f, 
        sideCollisionSlowDown = 1f, xCastDistance = 0.1f, zCastDistance = 0.2f;
    public WheelCollider frontLeftWheel, frontRightWheel, backWheels;
    public Transform frontLeftTransform, frontRightTransform, backWheelsTransform;
    Vector3 _startPos, _startRot, _bouncePos, _startBouncePos, _targetBouncePos;
    public bool bBounceEnabled = true;
    [HideInInspector]
    public bool bIsBouncing = false, bCrashed = false; 
    public float maxAliveHeight = 10f, minAliveHeight = -5f;
    public SnCSessionManager sessionManager;
    BoxCollider _boxCollider;

    private void Awake()
    {
        _startPos = transform.position;
        _startRot = new Vector3 (transform.rotation.x, transform.rotation.y, transform.rotation.z);
    }

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
    }

    public void CustomUpdate()
    {
        CheckInput();
        CheckCollisions();
        SetSteeringAngle();
        UpdateAcceleration();
        UpdateWheels();
        CheckDrift();
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
        _torqueInputValue = Mathf.Clamp(newValue, 0f, 1f);
    }

    float _driveDirection = 0f;
    bool _bForward = true;
    void UpdateAcceleration() 
    {
        float torque = 0f;
        if (!bIsBouncing)
        {
            //Drive on Input
            if (_yInput != 0f && SnCSessionManager.bInputEnabled)
            {
                //Set Direction when being still
                if (_torqueInputValue == 0f)
                {
                    _driveDirection = Mathf.Sign(_yInput);
                    _bForward = _driveDirection > 0f;
                    SetTorqueInputValue(_torqueInputValue + Time.deltaTime);
                }
                //Break when pressing opposite direction of current movement
                else if ((_yInput < 0f && _bForward) || (_yInput > 0f && !_bForward))
                {
                    frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = motorTorque.brakeTorque;
                    SetTorqueInputValue(0f);
                }
                //Accelerating
                else if (_driveDirection != 0f)
                {
                    _torqueInputValue += Time.deltaTime;
                    frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = 0f;
                }
                torque = motorTorque.torqueCurve.Evaluate(_torqueInputValue) * motorTorque.maxSpeed * _yInput;
            }
            //Keep rolling when no input any more
            else
                SetTorqueInputValue(_torqueInputValue - Time.deltaTime);
        }

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

    float _preDriftTimer = 0f;
    void CheckDrift() 
    {
        if (IsAtHighSpeed(10f) && _xInput != 0f)
        {
            _preDriftTimer += Time.deltaTime;
            if (_preDriftTimer > preDriftTime && _yInput > 0f)
            {
                Vector3 frontCarPos = transform.TransformPoint(0f, 0f, GetComponent<BoxCollider>().size.z);
                transform.RotateAround(frontCarPos, Vector3.up, driftSpeed * Mathf.Sign(_xInput) * Time.deltaTime);
            }
        }
        else
            _preDriftTimer = 0f;
    }

    bool IsAtHighSpeed(float highSpeedThreshold)
    {
        return Mathf.Abs(frontLeftWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold || Mathf.Abs(frontRightWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold;
    }

    void CheckCollisions()
    {
        //Checking collision with pavement, cop cars etc.
        CheckFrontBackCollision();
        //CheckSideCollision();
        //Falling Down or moving Too High with square
        if (transform.position.y > maxAliveHeight || transform.position.y < minAliveHeight)
            CrashCar();
    }

    void CheckFrontBackCollision() 
    {
        RaycastHit raycastHit;

        Vector3 frontCenter = transform.TransformPoint(_boxCollider.center + new Vector3(0, 0, _boxCollider.size.z / 2f));
        Vector3 backCenter = transform.TransformPoint(_boxCollider.center + new Vector3(0, 0, -_boxCollider.size.z / 2f));
        
        //Front Collision
        if (Physics.Raycast(frontCenter, transform.TransformDirection(Vector3.left), out raycastHit, xCastDistance*2f) || Physics.Raycast(frontCenter, transform.TransformDirection(Vector3.right), out raycastHit, xCastDistance * 2f))
        {
            if (raycastHit.collider.gameObject.CompareTag("Pavement"))
            {
                StartBounceBack(transform.TransformDirection(Vector3.back), 0f);
            }
        }

        //Back
        else if (Physics.Raycast(backCenter, transform.TransformDirection(Vector3.left), out raycastHit, xCastDistance * 2f) || Physics.Raycast(backCenter, transform.TransformDirection(Vector3.right), out raycastHit, xCastDistance * 2f))
        {
            if (raycastHit.collider.gameObject.CompareTag("Pavement"))
            {
                StartBounceBack(transform.TransformDirection(Vector3.forward), 0f);
            }
        }
    }

    void CheckSideCollision() 
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(transform.TransformPoint(0f, 0.5f, 0f), transform.TransformDirection(Vector3.left), out raycastHit, xCastDistance))
        {
            if (raycastHit.collider.gameObject.CompareTag("Pavement"))
            {
                //StartBounceBack(Vector3.right, frontLeftWheel.motorTorque - sideCollisionSlowDown * Time.deltaTime);
            }
        }
        else if (Physics.Raycast(transform.TransformPoint(0f, 0.5f, 0f), transform.TransformDirection(Vector3.right), out raycastHit, xCastDistance))
        {
            if (raycastHit.collider.gameObject.CompareTag("Pavement"))
            {
                //StartBounceBack(Vector3.left, frontRightWheel.motorTorque - sideCollisionSlowDown * Time.deltaTime);
            }
        }
    }

    public void CrashCar() 
    {
        bCrashed = true;
        sessionManager.EnableInput(false);
        SnCSessionManager.bInputEnabled = false;
        sessionManager.UIRef.SetCountdownUI("Crash!", Color.yellow);
        sessionManager.UIRef.SetRestartText(true);
    }

    void StartBounceBack(Vector3 bounceDirection, float slowedTorqueValue) 
    {
        if (bBounceEnabled && !bIsBouncing)
        {
            GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newCube.GetComponent<BoxCollider>().enabled = false;
            newCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            newCube.transform.position = transform.position + bounceDirection*bounceBackRange;
            bIsBouncing = true;
            _startBouncePos = transform.position;
            _targetBouncePos = transform.position + bounceDirection * bounceBackRange;
            _bounceTimer = 0f;
            SetTorqueInputValue(0f);
        }
    }

    float _bounceTimer = 0f;
    int _bounceRotDir = 0;
    void UpdateBounceBack()
    {
        _bounceTimer += Time.fixedDeltaTime;
        _bouncePos = Vector3.Lerp(_startBouncePos, _targetBouncePos , _bounceTimer / bounceTime);
        transform.position = _bouncePos;
        transform.Rotate(0f, bounceRotSpeed*Time.deltaTime*_bounceRotDir, 0f);
        if (_bounceTimer >= bounceTime)
        {
            bIsBouncing = false;
        }
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
