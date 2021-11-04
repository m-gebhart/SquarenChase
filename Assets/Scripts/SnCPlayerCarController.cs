using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCPlayerCarController : SnCCarBehaviour
{
    public SnCSessionManager sessionManager;
    [Header("Player Control")]
    bool _bIsJumping = false;
    public float maxSteeringAngle = 40f, steeringForce = 0.8f, driftSpeed = 20f, preDriftTime = 1f, saveJumpSpeed = 50f, saveJumpHeight = 0.01f, saveJumpUpTime = 0.25f;
    [Header("Crash")]
    public bool bCanCrash = true;
    public float maxAliveHeight = 10f, minAliveHeight = -5f, maxRotation = 35f;

    protected override void Awake()
    {
        base.Awake();
        PlayTireEffects(false);
        SetCarState(ECarState.Idle);
    }

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
            SetCarState(ECarState.Jumping);
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
        SetCarState(ECarState.Accelerating);
    }

    protected new void SetSteeringAngle()
    {
        _steeringAngle = maxSteeringAngle * _xInput * steeringForce;
        frontLeftWheel.steerAngle = frontRightWheel.steerAngle = _steeringAngle;
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
            sessionManager.EnableInput(false);
            SnCSessionManager.bInputEnabled = false;
            sessionManager.UIRef.SetCountdownUI("Crash!", Color.yellow);
            sessionManager.UIRef.SetRestartText(true);
            SetCarState(ECarState.Crashed);
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pavement") && !IsCarState(ECarState.Bouncing))
        {
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

        if (collisionSide == ECollisionSide.left)
            _rotDirection = 1f;
        else if (collisionSide == ECollisionSide.right)
            _rotDirection = -1f;

        SetCarState(ECarState.Bouncing);
    }

    public void ResetPlayerCar()
    {
        transform.position = _startPos;
        transform.rotation = new Quaternion(_startRot.x, _startRot.y, _startRot.z, 1f);
        GetComponent<Rigidbody>().useGravity = false;
        SetCarState(ECarState.Idle);
    }
}
