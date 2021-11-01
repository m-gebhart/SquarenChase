using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCCarBehaviour : MonoBehaviour
{
    [System.Serializable]
    public struct MotorTorque
    {
        public AnimationCurve torqueCurve;
        public float maxSpeed;
        public float accelerationTime;
        public float brakeTorque;
    }

    [Header("Acceleration")]
    public MotorTorque motorTorque;
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

    [HideInInspector]
    public bool bIsBouncing = false, bIsBraking = false, bCrashed = false;

    protected virtual void Awake()
    {
        _startPos = transform.position;
        _startRot = new Vector3(transform.rotation.x, transform.rotation.y, transform.rotation.z);
        _boxCollider = GetComponent<BoxCollider>();
        _rigidbody = GetComponent<Rigidbody>();
        PlayTireEffects(false);
    }

    protected virtual void CheckInput()
    {
    }

    protected virtual void SetSteeringAngle()
    {
    }

    public virtual void CustomUpdate() 
    {
        UpdateAcceleration();
        UpdateWheels();
    }


    float _torqueInputValue = 0f;
    protected void SetTorqueInputValue(float newValue)
    {
        _torqueInputValue = Mathf.Clamp(newValue, 0f, 1f);
    }


    /*----- ACCELERATION AND MOTOR TORQUE -----*/

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
                bIsBraking = false;
                _bForward = _driveDirection > 0f;
                SetTorqueInputValue(_torqueInputValue + Time.deltaTime);
            }
            //Break when pressing opposite direction of current movement
            else if ((_yInput < 0f && _bForward) || (_yInput > 0f && !_bForward))
            {
                bIsBraking = true;
                frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = motorTorque.brakeTorque;
                SetTorqueInputValue(0f);
            }
            //Acceleration forward
            else if (_driveDirection != 0f && !bIsBraking)
            {
                _torqueInputValue += Time.deltaTime;
                frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = 0f;
            }
        }
        //Keep braking until key release
        else if (_yInput == 0f && bIsBraking)
            bIsBraking = false;
        //Keep rolling when no input any more
        else
            SetTorqueInputValue(_torqueInputValue - Time.deltaTime);
        float torque = motorTorque.torqueCurve.Evaluate(_torqueInputValue / motorTorque.accelerationTime) * motorTorque.maxSpeed * _yInput;
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = torque;
        currentTorque = torque;
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
        bIsBouncing = false;
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
