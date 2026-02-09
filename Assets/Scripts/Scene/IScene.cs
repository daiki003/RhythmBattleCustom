using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public interface IScene
{
    public UniTask InitAsync(SceneInfoBase lastSceneInfo, SceneInfoBase nextSceneInfo, Image fadePanel);
    public UniTask StartSceneAsync();
    public UniTask Restart();
    public UniTask Pause();
    public UniTask DisposeAsync();
}

public class SceneInfoBase
{
    public virtual SceneType SceneType => SceneType.None;
    public bool IsAdditional;
    public TutorialCommandList TutorialCommand;
}

public class BattleSceneInfo : SceneInfoBase
{
    public override SceneType SceneType => SceneType.Battle;
    public SingleStageMaster StageMaster;
    public bool IsPractice;
    public float TimeRate;
}

public class TitleSceneInfo : SceneInfoBase
{
    public override SceneType SceneType => SceneType.Title;
}

public class HomeSceneInfo : SceneInfoBase
{
    public override SceneType SceneType => SceneType.Home;
}

public class ScoreMakerSceneInfo : SceneInfoBase
{
    public override SceneType SceneType => SceneType.ScoreMaker;
    public SingleStageMaster StageMaster;
}

public static class SceneInfoExtension
{
    
}
