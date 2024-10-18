using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Critical : MonoBehaviour
{
    [SerializeField] private Text text;
    private const float _fadeInTime = 0.3f;
    private const float _fadeOutTime = 0.3f;
    private const float _liveTime = 0.5f;

    public async UniTask InitAndStart(HitType hitType)
    {
        switch (hitType)
        {
            case HitType.Critical:
                text.text = "Critical";
                text.color = new Color32(255, 69, 0, 255);
                text.fontSize = 60;
                break;
            case HitType.Hit:
                text.text = "Hit";
                text.color = new Color32(255, 255, 0, 255);
                text.fontSize = 70;
                break;
        }

        transform.localScale = Vector3.zero;
        await transform.DOScale(1, _fadeInTime);
        await UniTask.WaitForSeconds(_liveTime);
        await transform.DOScale(0, _fadeOutTime);
        Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
