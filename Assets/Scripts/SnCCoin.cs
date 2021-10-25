using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCCoin : MonoBehaviour
{
    public float rotateSpeed = 50f, floatRange = 0.1f, floatSpeed = 1f;
    Vector3 startPos = Vector3.zero;
    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0, 0f, rotateSpeed * Time.deltaTime);
        transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time*floatSpeed)*floatRange, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        SnCSessionManager.score++;
        Destroy(this.gameObject);
    }
}
