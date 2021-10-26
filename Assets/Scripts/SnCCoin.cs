using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCCoin : MonoBehaviour
{
    public float rotateSpeed = 50f, floatRange = 0.1f, floatSpeed = 1f;
    bool bAnimating = false;
    SnCSessionManager _sessionManager;

    public void InitializeCoin(bool bStartAnim, SnCSessionManager sessionManager) 
    {
        _sessionManager = sessionManager;
        bAnimating = bStartAnim;
    }

    private void FixedUpdate()
    {
        if (bAnimating)
        {
            transform.Rotate(0, 0f, rotateSpeed * Time.fixedDeltaTime);
            transform.localPosition = new Vector3(0f, 0f, Mathf.Sin(Time.time * floatSpeed) * floatRange);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        _sessionManager.SetScore(SnCSessionManager.score + 1);
        Destroy(this.gameObject);
    }
}
