using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SnCStaticLibrary
{
    public static int SmoothValueToSideValue(int inputSide) 
    {
        if (inputSide > 3)
            return inputSide - 4;
        else if (inputSide < 0)
            return inputSide + 4;
        return inputSide;
    }


    public static int GetNextEntrySideToCurrentExitSideValue(int currentExitSide)
    {
        switch (currentExitSide)
        {
            default: case 0: return 2;
            case 1: return 3;
            case 2: return 0;
            case 3: return 1;
        }
    }
}
