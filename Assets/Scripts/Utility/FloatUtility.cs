using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloatUtility
{
    public static float RoundDown(float baseNumber, int digits)
    {
        float returnValue = baseNumber * Mathf.Pow(10, digits);
        returnValue = Mathf.Floor(returnValue);
        returnValue = returnValue / Mathf.Pow(10, digits);
        return returnValue;
    }
}
