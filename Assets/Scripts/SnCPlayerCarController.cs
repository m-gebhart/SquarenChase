using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCPlayerCarController : MonoBehaviour
{
    float _xInput, _yInput, _steeringAngle, highSpeedThreshold = 5f;
    public float maxSteeringAngle = 40f, motorForce = 50f, bounceBackRange = 0.5f, bounceTime = 0.5f, minBounceLocalZPoint = 1.5f;
    bool bIsBouncing = false;
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
        frontLeftWheel.motorTorque = frontRightWheel.motorTorque = !bIsBouncing ? _yInput * motorForce : 0f;
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

        if (!bIsBouncing)
        {
            wheelTransform.position = pos;
            wheelTransform.rotation = rot;
        }
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
            {
                /*Debug.Log("BOUNCE");
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                cube.GetComponent<BoxCollider>().enabled = false;
                cube.transform.position = transform.position + collision.GetContact(0).normal * bounceBackRange;*/
                StartCoroutine(BounceAnimation(transform.position + collision.GetContact(0).normal * bounceBackRange));
            }//;
            //Vector3.MoveTowards(transform.position, transform.position + collision.GetContact(0).normal*bounceBackRange, bounceBackRange);
        }
    }

    bool IsAtHighSpeed() 
    {
        return Mathf.Abs(frontLeftWheel.motorTorque) > motorForce - highSpeedThreshold || Mathf.Abs(frontRightWheel.motorTorque) > motorForce - highSpeedThreshold;
    }

    bool ShouldBounceBack(Vector3 contactPosition) 
    {
        return Mathf.Abs(this.transform.InverseTransformPoint(contactPosition).z) > minBounceLocalZPoint;
    }

    IEnumerator BounceAnimation(Vector3 targetPos) 
    {
        Vector3 startPos = transform.position;
        bIsBouncing = true;
        float timer = 0f;
        while (bIsBouncing) 
        {
            yield return new WaitForFixedUpdate();
            timer += Time.fixedDeltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, timer/bounceTime);
            //TODO: transform.Rotate(0f, 0.03f, 0f, Space.World);
            //TODO: no bounce when side hit wall
            if (timer >= bounceTime)
                bIsBouncing = false;
        }
    }

    public void Reset()
    {
        transform.position = startPos;
        transform.rotation = new Quaternion(startRot.x, startRot.y, startRot.z, 1f);
        GetComponent<Rigidbody>().useGravity = false;
        bCrashed = false;
    }
}
