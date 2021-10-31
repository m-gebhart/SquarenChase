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
    public float brakeTorque;
}

public class SnCPlayerCarController : MonoBehaviour
{
    float _xInput, _yInput, _steeringAngle;
    [Header("Acceleration")]
    public MotorTorque motorTorque;
    public float maxSteeringAngle = 40f, steeringForce = 0.8f, driftSpeed = 20f, preDriftTime = 1f;
    [Header("Bounce")]
    public bool bBounceEnabled = true;
    public float frontBackBounceRange = 0.15f, sideBounceRange = 0.1f, bounceTime = 0.5f, bounceHeight = 0.1f, bounceRotSpeed = 1f, sideCollisionSlowDown = 0.5f;
    [Header("Collision Raycast")]
    public float castOffset = 0.01f;
    public float xCastDistance = 0.05f, zCastDistance = 0.15f;
    [Header("Crash")]
    public bool bCanCrash = true;
    public float maxAliveHeight = 10f, minAliveHeight = -5f, maxRotation = 35f;
    [Header("Scene Objects")]
    public SnCSessionManager sessionManager;
    public WheelCollider frontLeftWheel, frontRightWheel, backWheels;
    public Transform frontLeftTransform, frontRightTransform, backWheelsTransform;
    public ParticleSystem leftDriftEffect, rightDriftEffect;

    Vector3 _startPos, _startRot, _bouncePos, _startBouncePos, _targetBouncePos;
    BoxCollider _boxCollider;
    [HideInInspector]
    public bool bIsBouncing = false, bCrashed = false; 

    private void Awake()
    {
        _startPos = transform.position;
        _startRot = new Vector3 (transform.rotation.x, transform.rotation.y, transform.rotation.z);
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        CheckInput();
        //CheckCollisions();
    }

    void CheckInput()
    {
        _xInput = Input.GetAxis("Horizontal");
        _yInput = Input.GetAxis("Vertical");
    }

    public void CustomUpdate()
    {
        SetSteeringAngle();
        UpdateAcceleration();
        UpdateWheels();
        CheckDrift();
        CheckRotation();
    }

    void CheckRotation() 
    {
        //avoid over rotation
        float xClamped = Mathf.Clamp(transform.eulerAngles.x, -maxRotation, maxRotation);
        float zClamped = Mathf.Clamp(transform.eulerAngles.z, -maxRotation, maxRotation);
        if (transform.rotation.x > maxRotation || transform.rotation.x < -maxRotation || transform.rotation.z > maxRotation || transform.rotation.z < -maxRotation)
            transform.rotation = Quaternion.Euler(xClamped, transform.eulerAngles.y, zClamped);

    }

    void SetSteeringAngle() 
    {
        _steeringAngle = maxSteeringAngle * _xInput * steeringForce;
        frontLeftWheel.steerAngle =  frontRightWheel.steerAngle = _steeringAngle;
    }

    float _torqueInputValue = 0f;
    void SetTorqueInputValue(float newValue) 
    {
        _torqueInputValue = Mathf.Clamp(newValue, 0f, 1f);
        frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = 0f;
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = motorTorque.torqueCurve.Evaluate(_torqueInputValue / motorTorque.accelerationTime);
    }

    float _driveDirection = 0f;
    bool _bForward = true;
    void UpdateAcceleration() 
    {
        //Drive on Input
        if (_yInput != 0f && SnCSessionManager.bInputEnabled && !bIsBouncing)
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
            //Acceleration forward
            else if (_driveDirection != 0f)
            {
                _torqueInputValue += Time.deltaTime;
                frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = 0f;
            }
        }
        //Keep rolling when no input any more
        else
            SetTorqueInputValue(_torqueInputValue - Time.deltaTime);
        float torque = motorTorque.torqueCurve.Evaluate(_torqueInputValue / motorTorque.accelerationTime) * motorTorque.maxSpeed * _yInput;
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = torque;
    }

    private void UpdateWheels()
    {
        /*if (bIsBouncing)
            UpdateBounceBack();*/
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
        if (IsAtHighSpeed(10f) && _xInput != 0f && !bIsBouncing)
        {
            //Particle Effects
            if (!leftDriftEffect.isPlaying)
                leftDriftEffect.Play();
            if (!rightDriftEffect.isPlaying)
                rightDriftEffect.Play();

            //Drift Behaviour
            _preDriftTimer += Time.deltaTime;
            /*if (_preDriftTimer > preDriftTime && _yInput > 0f)
            {
                Debug.Log("drifing");
                Vector3 frontCarPos = transform.TransformPoint(0f, 0f, _boxCollider.size.z * 0.75f);
                transform.RotateAround(frontCarPos, Vector3.up, driftSpeed * Mathf.Sign(_xInput) * Time.deltaTime);
            }*/
        }
        else
        {
            if (leftDriftEffect.emission.enabled || rightDriftEffect.emission.enabled) {
                leftDriftEffect.Stop();
                rightDriftEffect.Stop();
            }
            _preDriftTimer = 0f;
        }
    }

    bool IsAtHighSpeed(float highSpeedThreshold)
    {
        return Mathf.Abs(frontLeftWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold || Mathf.Abs(frontRightWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold;
    }

    void CheckCrash()
    {
        //Falling Down or moving up with square
        if (transform.position.y > maxAliveHeight || transform.position.y < minAliveHeight)
            CrashCar();
    }

    public void CrashCar()
    {
        if (bCanCrash)
        {
            bCrashed = true;
            sessionManager.EnableInput(false);
            SnCSessionManager.bInputEnabled = false;
            sessionManager.UIRef.SetCountdownUI("Crash!", Color.yellow);
            sessionManager.UIRef.SetRestartText(true);
        }
    }

    /*void CheckFrontBackCollision() 
    {
        RaycastHit raycastHit;
        Vector3 frontCenter = transform.TransformPoint(_boxCollider.center + new Vector3(0, 0, _boxCollider.size.z / 2f + castOffset));
        Vector3 backCenter = transform.TransformPoint(_boxCollider.center + new Vector3(0, 0, -(_boxCollider.size.z / 2f + castOffset)));

        //Front Collision
        if (Physics.Raycast(frontCenter, transform.TransformDirection(Vector3.left), out raycastHit, xCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(Vector3.back), 0f, frontBackBounceRange);
        if (Physics.Raycast(frontCenter, transform.TransformDirection(Vector3.right), out raycastHit, xCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(Vector3.back), 0f, frontBackBounceRange);

        //Back Collision
        if (Physics.Raycast(backCenter, transform.TransformDirection(Vector3.left), out raycastHit, xCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(Vector3.forward), 0f, frontBackBounceRange);
        if (Physics.Raycast(backCenter, transform.TransformDirection(Vector3.right), out raycastHit, xCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(Vector3.forward), 0f, frontBackBounceRange);
    }

    void CheckSideCollision() 
    {
        RaycastHit raycastHit;
        Vector3 rightCenter = transform.TransformPoint(_boxCollider.center + new Vector3(_boxCollider.size.x / 2f + castOffset, 0, 0));
        Vector3 leftCenter = transform.TransformPoint(_boxCollider.center + new Vector3(-(_boxCollider.size.x / 2f + castOffset), 0, 0));

        //Right Collision
        Vector3 bounceToLeftDirection = IsAtHighSpeed(motorTorque.maxSpeed * 0.75f) ? new Vector3(-1, 0, 1) : Vector3.left;
        if (Physics.Raycast(rightCenter, transform.TransformDirection(Vector3.forward), out raycastHit, zCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(bounceToLeftDirection), frontRightWheel.motorTorque*sideCollisionSlowDown, sideBounceRange);
        if (Physics.Raycast(rightCenter, transform.TransformDirection(Vector3.back), out raycastHit, zCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(bounceToLeftDirection), frontRightWheel.motorTorque*sideCollisionSlowDown, sideBounceRange);


        //Left Collision
        Vector3 bounceToRightDirection = IsAtHighSpeed(motorTorque.maxSpeed * 0.75f) ? new Vector3(1, 0, 1) : Vector3.right;
        if (Physics.Raycast(leftCenter, transform.TransformDirection(Vector3.forward), out raycastHit, zCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(bounceToRightDirection), frontLeftWheel.motorTorque*sideCollisionSlowDown, sideBounceRange);
        if (Physics.Raycast(leftCenter, transform.TransformDirection(Vector3.back), out raycastHit, zCastDistance))
            BounceCollision(raycastHit, transform.TransformDirection(bounceToRightDirection), frontLeftWheel.motorTorque*sideCollisionSlowDown, sideBounceRange);
    }

    private void BounceCollision(RaycastHit raycastHit, Vector3 bounceDirection, float newTorquedValue, float bounceRange)
    {
        GameObject hitGameObject = raycastHit.collider.gameObject;
        if (hitGameObject.CompareTag("Pavement"))
            if (!hitGameObject.transform.parent.GetComponent<SnCSquare>().bIsShaking)
                GetComponent<Rigidbody>().AddForce(bounceDirection*bounceRange);
                //StartBounceBack(bounceDirection, newTorquedValue, bounceRange);
    }*/

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pavement"))
        {
            bIsBouncing = true;
            StartCoroutine(ResetBounce());
            GetComponent<Rigidbody>().AddForce(collision.GetContact(0).normal * frontBackBounceRange * frontLeftWheel.motorTorque);
        }
    }

    IEnumerator ResetBounce() 
    {
        yield return new WaitForSeconds(bounceTime);
        bIsBouncing = false;
    }

    /*float _bounceRotDir = 0f;
    void StartBounceBack(Vector3 bounceDirection, float slowedTorqueValue, float bounceRange) 
    {
        if (bBounceEnabled && !bIsBouncing)
        {
            GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newCube.GetComponent<BoxCollider>().enabled = false;
            newCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            newCube.transform.position = transform.position + bounceDirection* bounceRange;
            bIsBouncing = true;
            _startBouncePos = transform.position;
            _targetBouncePos = transform.position + bounceDirection * bounceRange;
            _bounceRotDir = bounceDirection.x;
            _bounceTimer = 0f;
            SetTorqueInputValue(slowedTorqueValue);
        }
    }

    float _bounceTimer = 0f;
    void UpdateBounceBack()
    {
        if (_bounceTimer >= bounceTime)
        {
            bIsBouncing = false;
            return;
        }
        _bounceTimer += Time.fixedDeltaTime;
        float height = 0;
        _bouncePos = Vector3.Lerp(_startBouncePos, _targetBouncePos, _bounceTimer / bounceTime) + new Vector3(0f, height, 0f);
        transform.position = _bouncePos;
        transform.Rotate(0f, bounceRotSpeed*Time.deltaTime*_bounceRotDir, 0f);
    }*/

    public void Reset()
    {
        transform.position = _startPos;
        transform.rotation = new Quaternion(_startRot.x, _startRot.y, _startRot.z, 1f);
        GetComponent<Rigidbody>().useGravity = false;
        bCrashed = false;
        bIsBouncing = false;
    }
}
