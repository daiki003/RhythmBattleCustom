using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMaskTarget : MonoBehaviour
{
    public string TargetId;

    void Awake()
    {
        TutorialManager.Instance.AddTargetRect(TargetId, transform as RectTransform);
    }
}
