using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloatExtension
{
    public static float RoundDown(this float baseNumber, int digits)
    {
        float returnValue = baseNumber * Mathf.Pow(10, digits);
        returnValue = Mathf.Floor(returnValue);
        returnValue = returnValue / Mathf.Pow(10, digits);
        return returnValue;
    }

    public static bool IsBetween(this float targetNumber, float startValue, float endValue)
    {
        return startValue < targetNumber && targetNumber < endValue;
    }
}
