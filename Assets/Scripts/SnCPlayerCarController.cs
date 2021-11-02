using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCPlayerCarController : SnCCarBehaviour
{
    public SnCSessionManager sessionManager;
    [Header("Player Control")]
    public bool bDriftEnabled = true;
    public float maxSteeringAngle = 40f, steeringForce = 0.8f, driftSpeed = 20f, preDriftTime = 1f, saveJumpSpeed = 50f, saveJumpHeight = 0.01f, saveJumpUpTime = 0.25f;
    bool _bIsJumping = false;
    [Header("Crash")]
    public bool bCanCrash = true;
    public float maxAliveHeight = 10f, minAliveHeight = -5f, maxRotation = 35f;

    void Update()
    {
        CheckInput();
    }

    protected new void CheckInput()
    {
        _xInput = Input.GetAxis("Horizontal");
        _yInput = Input.GetAxis("Vertical");
    }

    public override void CustomUpdate()
    {
        CheckSaveAction();
        SetSteeringAngle();
        base.CustomUpdate();
        CheckPlayerDrift();
        CheckPlayerCrash();
    }

    bool SurpassedMaxRotation() 
    {
        return (transform.eulerAngles.z > maxRotation && transform.eulerAngles.z-360f < -maxRotation) || (transform.eulerAngles.x > maxRotation && transform.eulerAngles.x - 360f < -maxRotation);
    }

    void CheckSaveAction() 
    {
        //if car is upside down
        if (SurpassedMaxRotation())
        {
            if (GetSaveActionInput())
                transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
        }

        //if car is stuck on edge
        else if ((!frontLeftWheel.isGrounded && !frontRightWheel.isGrounded) || !backWheels.isGrounded)
        {
            if (GetSaveActionInput())
            {
                StartJump();
            }
        }
        else
            sessionManager.UIRef.SetSaveActionText(false);

        if (_bIsJumping)
            JumpUpdate();
    }

    bool GetSaveActionInput() 
    {
        //Set UI Text
        if (!sessionManager.UIRef.IsSaveActionTextActive() && !sessionManager.spawnRef.bOnStartSquare)
            sessionManager.UIRef.SetSaveActionText(true);
        sessionManager.UIRef.saveActionWorldText.transform.parent.LookAt(Camera.main.transform.position);
        sessionManager.UIRef.saveActionWorldText.transform.parent.position = transform.position + new Vector3(0f, 0.3f, 0f);

        //GroundCar
        if (Input.GetKey("space"))
        {
            sessionManager.UIRef.SetSaveActionText(false);
            return true;
        }
        return false;
    }

    Vector3 _jumpTargetPos;
    void StartJump() 
    {
        if (!_bIsJumping)
        {
            // start Jump
            _bIsJumping = true;
            _jumpTargetPos = transform.position + transform.TransformDirection(0f, 0.1f, _boxCollider.size.z * _yInput);
            Debug.DrawRay(_jumpTargetPos, Vector3.up, Color.yellow);
            Debug.Log("line drawn");
        }
        StartCoroutine("ResetJump");
    }

    void JumpUpdate() 
    {
        transform.Translate(new Vector3(0f, saveJumpHeight * Time.fixedDeltaTime, saveJumpSpeed * Time.fixedDeltaTime * _yInput));
    }

    IEnumerator ResetJump() 
    {
        yield return new WaitForSeconds(saveJumpUpTime);
        _bIsJumping = false;
    }

    protected new void SetSteeringAngle()
    {
        _steeringAngle = maxSteeringAngle * _xInput * steeringForce;
        frontLeftWheel.steerAngle = frontRightWheel.steerAngle = _steeringAngle;
    }

    float _preDriftTimer = 0f;
    void CheckPlayerDrift() 
    {
        if (bDriftEnabled)
        {
            if (IsAtHighSpeed(10f) && _xInput != 0f && !bIsBouncing)
            {
                //Particle Effects
                PlayTireEffects(true);

                //Drift Behaviour
                _preDriftTimer += Time.deltaTime;
                if (_preDriftTimer > preDriftTime && _yInput > 0f)
                {
                    Vector3 frontCarPos = transform.TransformPoint(0f, 0f, frontLeftWheel.transform.position.z);
                    transform.RotateAround(frontCarPos, Vector3.up, driftSpeed * Mathf.Sign(_xInput) * Time.deltaTime);
                }
            }
            else
            {
                PlayTireEffects(false);
                _preDriftTimer = 0f;
            }
        }
    }

    void CheckPlayerCrash()
    {
        //Falling Down or moving up with square
        if (transform.position.y > maxAliveHeight || transform.position.y < minAliveHeight)
            CrashPlayerCar();
    }

    public void CrashPlayerCar()
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


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pavement") && !bIsBouncing)
        {
            bIsBouncing = true;
            StartCoroutine(ResetBounce());
            BounceBackPlayerCar(collision);
        }
    }

    void BounceBackPlayerCar(Collision collision) 
    {
        SnCSessionManager.bInputEnabled = false;
        ECollisionSide collisionSide = GetCollisionSide(collision.GetContact(0).point);
        float bounceRange = collisionSide == ECollisionSide.left || collisionSide == ECollisionSide.right ? sideBounceRange : frontBackBounceRange;

        _rigidbody.AddForce(collision.GetContact(0).normal * bounceRange * currentTorque);

        float rotDirection = 0f;
        if (collisionSide == ECollisionSide.left)
            rotDirection = 1f;
        else if (collisionSide == ECollisionSide.right)
            rotDirection = -1f;

        transform.Rotate(0f, bounceRotSpeed * Time.fixedDeltaTime * rotDirection, 0f);
    }

    public void ResetPlayerCar()
    {
        transform.position = _startPos;
        transform.rotation = new Quaternion(_startRot.x, _startRot.y, _startRot.z, 1f);
        GetComponent<Rigidbody>().useGravity = false;
        bCrashed = false;
        bIsBouncing = false;
    }
}
