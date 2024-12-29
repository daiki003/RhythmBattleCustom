using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonWithBacklight : MonoBehaviour
{
    [SerializeField] private Image _backlightImage;
    [SerializeField] private Button _button;
    public Button Button => _button;
    public void SetBacklight(bool isActive)
    {
        _backlightImage.gameObject.SetActive(isActive);
    }
}
