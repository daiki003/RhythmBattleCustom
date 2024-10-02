using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class Ball : MonoBehaviour
{
    [SerializeField] private Image _ballImage;

    private const float beforeActiveTime = 1.65f;
    private const float activeTime = 0.1f;
    public bool IsActive;
    public bool IsLeft;
    public CancellationTokenSource Cts;

    public void Init(bool isleft, CancellationTokenSource cts)
    {
        _ballImage.color = Color.red;
        IsLeft = isleft;
        Cts = cts;
        CountDown().Forget();
    }

    public async UniTask CountDown()
    {
        await UniTask.WaitForSeconds(beforeActiveTime);
        IsActive = true;
        await UniTask.WaitForSeconds(activeTime);
        IsActive = false;
    }
}
