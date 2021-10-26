using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCCoin : MonoBehaviour
{
    public float rotateSpeed = 50f, floatRange = 0.1f, floatSpeed = 1f;
    bool bAnimating = false;
    Vector3 startPos = Vector3.zero;
    SnCSessionManager _sessionManager;

    public void InitializeCoin(bool bStartAnim, SnCSessionManager sessionManager) 
    {
        _sessionManager = sessionManager;
        startPos = transform.parent.localPosition;
        bAnimating = bStartAnim;
    }

    private void FixedUpdate()
    {
        if (bAnimating)
        {
            transform.Rotate(0, 0f, rotateSpeed * Time.fixedDeltaTime);
            //transform.localPosition = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatRange, 0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        _sessionManager.SetScore(SnCSessionManager.score + 1);
        Destroy(this.gameObject);
    }
}
