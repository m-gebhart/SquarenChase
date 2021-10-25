using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCScoreTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")) 
        {
            SnCSessionManager.score++;
            Debug.Log(SnCSessionManager.score);
            Destroy(this.gameObject);
        }
    }
}
