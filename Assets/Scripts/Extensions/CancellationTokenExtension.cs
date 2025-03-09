using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public static class CancellationTokenExtension
{
    public static void CancelAndDispose(this CancellationTokenSource cts)
    {
        cts.Cancel();
        cts.Dispose();
    }
}
