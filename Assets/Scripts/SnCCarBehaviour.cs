using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCCarBehaviour : MonoBehaviour
{
    [System.Serializable]
    public struct SnCMotorTorque
    {
        public AnimationCurve torqueCurve;
        public float maxSpeed;
        public float accelerationTime;
        public float brakeTorque;
    }

    public enum ECarState
    {
        Idle, //no movement
        Accelerating, //forward and backwards
        Braking,
        Bouncing,
        Jumping,
        Crashed, //player death
    }

    [Header("Acceleration")]
    public SnCMotorTorque motorTorque;
    protected float _xInput, _yInput, _steeringAngle = 40f;
    [HideInInspector]
    public float currentTorque;
    [Header("Bounce")]
    public bool bBounceEnabled = true;
    public float frontBackBounceRange = 0.15f, sideBounceRange = 0.1f, bounceTime = 0.5f, bounceHeight = 0.1f, bounceRotSpeed = 1f;
    [Header("Scene Objects")]
    public WheelCollider frontLeftWheel, frontRightWheel, backWheels;
    public Transform frontLeftTransform, frontRightTransform, backWheelsTransform;
    public ParticleSystem leftDriftEffect, rightDriftEffect;

    protected Vector3 _startPos, _startRot;
    protected BoxCollider _boxCollider;
    protected Rigidbody _rigidbody;

    [SerializeField]
    protected ECarState _currentCarState = ECarState.Idle;
    protected ECarState _previousCarState; 

    /*----- CAR STATE MANAGEMENT -----*/
    public void SetCarState(ECarState newState) 
    {
        if (_currentCarState == ECarState.Crashed && ECarState.Idle != newState)
            return;
        _previousCarState = _currentCarState;
        _currentCarState = newState;
    }

    public ECarState GetCarState() { return _currentCarState; }

    public bool IsCarState(ECarState state) { return _currentCarState == state; }

    public bool IsPreviousCarState(ECarState state) { return _previousCarState == state; }

    protected virtual void Awake()
    {
        _startPos = transform.position;
        _startRot = new Vector3(transform.rotation.x, transform.rotation.y, transform.rotation.z);
        _boxCollider = GetComponent<BoxCollider>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void CheckInput()
    {
        //to be overriden by player
    }

    protected virtual void SetSteeringAngle()
    {
        //to be overriden
    }

    public virtual void CustomUpdate() 
    {
        UpdateAcceleration();
        UpdateRotation();
        UpdateWheels();
    }


    float _torqueInputValue = 0f;
    protected void SetTorqueInputValue(float newValue)
    {
        _torqueInputValue = Mathf.Clamp(newValue, 0f, 1f);
    }


    /*----- ACCELERATION AND MOTOR TORQUE -----*/

    public void SetMotorForce(float newMotorTorque) 
    {
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = newMotorTorque;
        currentTorque = newMotorTorque;
    }

    public void SetBrakeForce(float newBrakeTorque)
    {
        frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = newBrakeTorque;
        SetMotorForce(0f);
    }

    float _driveDirection = 0f;
    bool _bForward = true;
    void UpdateAcceleration()
    {
        //Drive on Input
        if (_yInput != 0f && SnCSessionManager.bInputEnabled)
        {
            if (!IsCarState(ECarState.Bouncing) && !IsCarState(ECarState.Braking))
            {
                //Set Direction when being still
                if (IsCarState(ECarState.Idle))
                {
                    _driveDirection = Mathf.Sign(_yInput);
                    _bForward = _driveDirection > 0f;
                    SetTorqueInputValue(_torqueInputValue + Time.deltaTime);
                    SetCarState(ECarState.Accelerating);
                }
                //Start Braking when pressing opposite direction of current movement
                else if ((_yInput < 0f && _bForward) || (_yInput > 0f && !_bForward))
                {
                    SetBrakeForce(motorTorque.brakeTorque);
                    SetTorqueInputValue(0f);
                    SetCarState(ECarState.Braking);
                }
                //Acceleration forward
                else if (_driveDirection != 0f && !IsCarState(ECarState.Braking))
                {
                    _torqueInputValue += Time.deltaTime;
                }
            }
            //reset braking / acceleration on full stop
            else if (transform.InverseTransformDirection(_rigidbody.velocity).z <= 0.001f || -transform.InverseTransformDirection(_rigidbody.velocity).z >= -0.001f)
                ResetAcceleration();

            else if (_yInput == 0f)
                //stop braking on brake release
                if (IsCarState(ECarState.Braking))
                    ResetAcceleration();
                //Keep rolling on acceleration release
                else
                    SetTorqueInputValue(_torqueInputValue - Time.deltaTime);

            float torque = motorTorque.torqueCurve.Evaluate(_torqueInputValue / motorTorque.accelerationTime) * motorTorque.maxSpeed * _yInput;
            SetMotorForce(torque);
        }
    }

    void ResetAcceleration()
    {
        _driveDirection = 0f;
        _torqueInputValue = 0f;
        SetBrakeForce(0f);
        SetMotorForce(0f);
        SetCarState(ECarState.Idle);
    }

    protected float _rotDirection = 0f;
    protected void UpdateRotation()
    {
        if (GetCarState() == ECarState.Bouncing)
            transform.Rotate(0f, bounceRotSpeed * Time.fixedDeltaTime * _rotDirection, 0f);
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

    protected bool IsAtHighSpeed(float highSpeedThreshold)
    {
        return Mathf.Abs(frontLeftWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold || Mathf.Abs(frontRightWheel.motorTorque) > motorTorque.maxSpeed - highSpeedThreshold;
    }


    /*----- CAR COLLISION -----*/
    protected enum ECollisionSide
    {
        left, front, right, back
    };
    ECollisionSide[] _collisionSides = new ECollisionSide[4] { ECollisionSide.left, ECollisionSide.front, ECollisionSide.right, ECollisionSide.back };

    protected ECollisionSide GetCollisionSide(Vector3 worldCollisionPoint)
    {
        ECollisionSide shortestside = ECollisionSide.left;
        for (int side = 0; side < 4; side++)
        {
            if (GetDistanceToSide(worldCollisionPoint, shortestside) > GetDistanceToSide(worldCollisionPoint, _collisionSides[side]))
                shortestside = _collisionSides[side];
        }

        return shortestside;
    }

    float GetDistanceToSide(Vector3 worldDistancePoint, ECollisionSide side)
    {
        switch (side)
        {
            case ECollisionSide.left:
            default:
                return Vector2.Distance(worldDistancePoint, transform.TransformPoint(new Vector3(-_boxCollider.size.x, 0f, 0f)));
            case ECollisionSide.front:
                return Vector2.Distance(worldDistancePoint, transform.TransformPoint(new Vector3(0, 0f, _boxCollider.size.z)));
            case ECollisionSide.right:
                return Vector2.Distance(worldDistancePoint, transform.TransformPoint(new Vector3(_boxCollider.size.x, 0f, 0f)));
            case ECollisionSide.back:
                return Vector2.Distance(worldDistancePoint, transform.TransformPoint(new Vector3(0, 0f, -_boxCollider.size.z)));
        }
    }



    protected IEnumerator ResetBounce()
    {
        yield return new WaitForSeconds(bounceTime);
        SnCSessionManager.bInputEnabled = true;
        if (IsPreviousCarState(ECarState.Accelerating) || IsPreviousCarState(ECarState.Braking))
            SetCarState(_previousCarState);
        else
            SetCarState(ECarState.Accelerating);
    }


    /*----- PARTICLE SYSTEMS -----*/

    public virtual void PlayTireEffects(bool bPlay)
    {
        if (bPlay)
        {
            if (!leftDriftEffect.isPlaying)
                leftDriftEffect.Play();
            if (!rightDriftEffect.isPlaying)
                rightDriftEffect.Play();
        }
        else
        {
            if (leftDriftEffect.isPlaying)
                leftDriftEffect.Stop();
            if (rightDriftEffect.isPlaying)
                rightDriftEffect.Stop();
        }
    }
}
