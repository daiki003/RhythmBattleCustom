using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;

public class ControlPanelPageBase : MonoBehaviour
{
    protected Subject<ControlPanelRequestBase> _onRequest = new();
    public Observable<ControlPanelRequestBase> OnRequest => _onRequest;

    public virtual void Init()
    {
        
    }

    public virtual void SetMusicParameter(MusicParameter musicParameter)
    {
        
    }

    public virtual void SetButtonState(bool isSelectedLine, bool isCopiedLine, bool isExsistPastLine)
    {
        
    }

    private void OnDestroy() {
        _onRequest.Dispose();
        _onRequest = null;
    }
}
