using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnCSquare : MonoBehaviour
{
    [Header("Free Entry/Exit Sides")]
    [HideInInspector]
    public int squareDirection = 0; /*max = 3; 0 is default orientation; +1 = +90 degree rotate*/
    public bool bLeft = false, bTop = false, bRight = false, bBottom = false;
    public float verticalOffset = 0f, postVerticalOffset = 0f;
    [HideInInspector]
    public int exitSide = -1, entrySide = -1;
    [Header("Creation Animation")]
    public AnimationCurve creationYCurve;
    public float creationHeight, creationTime;
    [Header("Shake Animation")]
    public bool bShouldShake = true;
    bool bShakeCompleted = false;
    public float shakeTime, shakeRange, shakeIntensity;
    [Header("Removal Animation")]
    public AnimationCurve removalYCurve;
    public float removalHeight, removalTime;

    public void Rotate(int steps)
    {
        ChangeSquareDirection(steps);
        ChangeFreeSides(steps);
        transform.Rotate(0, 90f * steps, 0, Space.World);
    }

    void ChangeSquareDirection(int steps) 
    {
        squareDirection += SnCStaticLibrary.SmoothValueToSideValue(steps);
    }

    void ChangeFreeSides(int steps) 
    {
        for (int i = 0; i < Mathf.Abs(steps); i++)
        {
            if (steps > 0)
            {
                bool tempLeft = bLeft;
                bLeft = bBottom;
                bBottom = bRight;
                bRight = bTop;
                bTop = tempLeft;
            }
            else if (steps < 0)
            {
                bool tempLeft = bLeft;
                bLeft = bTop;
                bTop = bRight;
                bRight = bBottom;
                bBottom = tempLeft;
            }
        }
    }

    /*
     * 0 = left,
     * 1 = top,
     * 2 = right,
     * 3 = bottom;
     */
    public bool IsSideFree(int side) 
    {
        switch (side) 
        {
            case 0: default: 
                return bLeft;
            case 1:
                return bTop;
            case 2:
                return bRight;
            case 3:
                return bBottom;
        }
    }

    public int GetAnyFreeSide()
    {
        int tempRandom = Random.Range(0, 3);
        for (int i = 0; i < 4; i++)
        {
            tempRandom = SnCStaticLibrary.SmoothValueToSideValue(++tempRandom);
            if (IsSideFree(tempRandom) && tempRandom != entrySide)
                return tempRandom;
        }
        return 0;
    }

    public int GetStepsToNextFreeSide(int startSide, int direction) 
    {
        int tempCounter = 0;
        for (int i = 0; i < 4; i++)
        {
            if (IsSideFree(startSide + direction*tempCounter) && IsSideFree(entrySide)) 
            {
                return tempCounter;
            }
            tempCounter++;
        }
        return 0;
    }

    public void AnimateCreation() 
    {
        StartCoroutine(PlayHeightCurve(creationYCurve, transform.position, creationTime, -creationHeight, false, false));
    }

    public void AnimateRemoval(bool bShouldDestroy) 
    {
        if (bShouldShake)
            StartCoroutine("Shake");
        StartCoroutine(PlayHeightCurve(removalYCurve, transform.position, removalTime, removalHeight, bShouldShake, bShouldDestroy));
    }

    IEnumerator PlayHeightCurve(AnimationCurve curve, Vector3 startPos, float time, float height, bool bShake, bool bDestroy) 
    {
        if (bShake)
            yield return new WaitUntil(() => bShakeCompleted == true);
        float timer = 0;
        Vector3 targetPos = transform.position + new Vector3(0, height, 0);
        while (time > timer)
        {
            yield return new WaitForFixedUpdate();
            timer += Time.fixedDeltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, curve.Evaluate(timer/time));
        }
        if (bDestroy)
            Destroy(this.gameObject);
    }

    IEnumerator Shake() 
    {
        float timer = 0;
        float xExtend = 0;
        float zExtend = 0;
        if (exitSide == 1 || exitSide == 3)
            xExtend = shakeIntensity;
        else
            zExtend = shakeIntensity;
        while (shakeTime > timer)
        {
            yield return new WaitForFixedUpdate();
            timer += Time.fixedDeltaTime;
            transform.position = new Vector3(
                transform.position.x + Mathf.Sin(timer * xExtend) * shakeRange,
                transform.position.y,
                transform.position.z + Mathf.Sin(timer * zExtend) * shakeRange);
        }
        bShakeCompleted = true;
    }

    public void Reset()
    {
        bShakeCompleted = false;
    }
}