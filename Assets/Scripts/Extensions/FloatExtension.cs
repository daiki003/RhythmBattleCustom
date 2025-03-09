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

    public static bool IsBetween(this float targetNumber, float startValue, float endValue, bool isIncludeBound = false)
    {
        if (isIncludeBound)
        {
            return startValue <= targetNumber && targetNumber <= endValue;
        }
        return startValue < targetNumber && targetNumber < endValue;
    }

    /// <summary>
    /// 小数点以下の桁数を取得
    /// </summary>
    public static int GetPrecision(this float value)
    {
        string priceString = value.ToString().TrimEnd('0');

        int index = priceString.IndexOf('.');
        if (index == -1) return 0;
        return priceString.Substring(index + 1).Length;
    }
}
