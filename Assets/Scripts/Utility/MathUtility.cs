using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtility
{
    public static bool IsDuaring(float targetValue, float minValue, float maxValue, bool isIncludeBorder = false)
    {
        if (isIncludeBorder)
        {
            return targetValue >= minValue && targetValue <= maxValue;
        }
        else
        {
            return targetValue > minValue && targetValue < maxValue;
        }
    }
}
