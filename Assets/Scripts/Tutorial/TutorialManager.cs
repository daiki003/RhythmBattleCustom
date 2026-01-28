using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

public static class TutorialDefine
{
    public class TutorialStripParam
    {
        public string Id;
        public string Title;
    }

    public static TutorialStripParam[] HomeTutorialIds = new TutorialStripParam[]
    {
        new TutorialStripParam{ Id = "home_001", Title = "ステージ選択、プレイ" },
        new TutorialStripParam{ Id = "home_002", Title = "カスタムタブ" },
    };

    public static TutorialStripParam[] PracticeTutorialIds = new TutorialStripParam[]
    {
        new TutorialStripParam{ Id = "practice_001", Title = "各種ボタンについて" },
        new TutorialStripParam{ Id = "practice_002", Title = "時間移動について" },
    };

    public static TutorialStripParam[] ScoreMakerTutorialIds = new TutorialStripParam[]
    {
        new TutorialStripParam{ Id = "scoremaker_001", Title = "ボールの配置" },
        new TutorialStripParam{ Id = "scoremaker_002", Title = "ライン選択" },
        new TutorialStripParam{ Id = "scoremaker_003", Title = "配置モード切替" },
        new TutorialStripParam{ Id = "scoremaker_004", Title = "コピー・ペースト" },
        new TutorialStripParam{ Id = "scoremaker_005", Title = "その他の操作" },
        new TutorialStripParam{ Id = "scoremaker_006", Title = "タイムジャンプ" },
        new TutorialStripParam{ Id = "scoremaker_007", Title = "演奏作成モード" },
        new TutorialStripParam{ Id = "scoremaker_008", Title = "速度設定" },
        new TutorialStripParam{ Id = "scoremaker_009", Title = "拍子設定" },
    };
}

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Transform _panelParent;

    public Dictionary<string, RectTransform> TargetRectDict { get; private set; }
    public Dictionary<string, Button> TargetButtonDict { get; private set; }

    private TutorialPanel _currentTutorialPanel;
    private TutorialCommandList[] _allCommandList;

    private const string _prefabPath = "Tutorial/TutorialPanel";
    private const string _commandListPath = "ScriptableObject/Tutorial";

    public bool IsDuringTutorial => _currentTutorialPanel != null;

    public static TutorialManager Instance;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        TargetRectDict = new Dictionary<string, RectTransform>();
        TargetButtonDict = new Dictionary<string, Button>();
        _allCommandList = Resources.LoadAll<TutorialCommandList>(_commandListPath);
    }

    public TutorialCommandList[] GetTutorialCommandLists(TutorialType tutorialType)
    {
        return _allCommandList.Where(x => x.TutorialType == tutorialType).ToArray();
    }

    public async UniTask StartTutorialAsync(TutorialCommandList commandList)
    {
        _currentTutorialPanel = Instantiate(ResourceManager.LoadPrefab<TutorialPanel>(_prefabPath), Instance._panelParent);
        _currentTutorialPanel.Init();
        await _currentTutorialPanel.PlayTutorialAsync(commandList);
        _currentTutorialPanel.Dispose();
        _currentTutorialPanel = null;
    }

    public void AdvanceStep(string advanceId)
    {
        _currentTutorialPanel?.AdvanceStepById(advanceId);
    }

    public bool TryGetTargetRect(string id, out RectTransform target)
    {
        return TargetRectDict.TryGetValue(id, out target);
    }

    public bool TryGetTargetButton(string id, out Button target)
    {
        return TargetButtonDict.TryGetValue(id, out target);
    }

    public void AddTargetRect(string id, RectTransform target)
    {
        TargetRectDict[id] = target;
        if (target.TryGetComponent<Button>(out var button))
        {
            TargetButtonDict[id] = button;
        }
    }

    public RectTransform GetTargetRect(TutorialTarget target)
    {
        if (target == null)
        {
            return null;
        }
        if (!TryGetTargetRect(target.TargetId, out var rect))
        {
            return null;
        }
        return rect;
    }
}
