using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCPlayerCarController : SnCCarBehaviour
{
    public SnCSessionManager sessionManager;
    [Header("Player Control")]
    public bool bDriftEnabled = true;
    public float maxSteeringAngle = 40f, steeringForce = 0.8f, driftSpeed = 20f, preDriftTime = 1f;
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
        if (Input.GetKeyDown("space"))
            CheckRotation();
    }

    void CheckRotation() 
    {
        //Avoid Upside Down position
        if (transform.eulerAngles.z > maxRotation || transform.eulerAngles.z < -maxRotation)
        {
            transform.eulerAngles =new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }
    }

    public override void CustomUpdate()
    {
        SetSteeringAngle();
        base.CustomUpdate();
        CheckPlayerDrift();
        CheckPlayerCrash();
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
        Debug.Log("Bounce");
        ECollisionSide collisionSide = GetCollisionSide(collision.GetContact(0).point);
        float bounceRange = collisionSide == ECollisionSide.left || collisionSide == ECollisionSide.right ? sideBounceRange : frontBackBounceRange;

        Debug.DrawLine(collision.GetContact(0).point + collision.GetContact(0).normal * bounceRange * currentTorque, Vector3.up*10f, Color.yellow, 10f);

        _rigidbody.AddForce(collision.GetContact(0).normal * bounceRange * currentTorque);

        float bounceDirection = 0f;
        if (collisionSide == ECollisionSide.left)
            bounceDirection = 1f;
        else if (collisionSide == ECollisionSide.right)
            bounceDirection = -1f;

        transform.Rotate(0f, bounceRotSpeed * Time.fixedDeltaTime * bounceDirection, 0f);
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
