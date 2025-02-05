using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerStrip : MonoBehaviour
{
    // [SerializeField] private Image _enemyImage;
    // [SerializeField] private Text _titleText;
    // [SerializeField] private List<Button> _editButtonList;
    // [SerializeField] private Button _createButton;
    // [SerializeField] private ButtonWithBacklight _bgmButton;
    // public ButtonWithBacklight BgmButton => _bgmButton;

    // private string _stageId;
    // private bool _isBgmPlaying;

    // public Subject<(string stageId, bool isPlay)> OnClickedBgmButton { get; private set; } = new Subject<(string, bool)>();

    // public void Init(string stageId, string stageName)
    // {
    //     var enemySprite = ResourceManager.LoadSpriteWithDummyEnemy("Enemy/" + stageId);
    //     _enemyImage.sprite = enemySprite;
    //     _titleText.text = stageName;
    //     _stageId = stageId;
    //     for (int i = 0; i < _editButtonList.Count; i++)
    //     {
    //         _editButtonList[i].OnClickAsObservable().Subscribe(async x =>
    //         {
    //             GameManager.instance.StartScoreMaker(_stageId, i);
    //         });
    //     }
    //     _createButton.OnClickAsObservable().Subscribe(async x =>
    //     {
    //         GameManager.instance.StartScoreMaker(_stageId);
    //     }).AddTo(this);
    //     _bgmButton.Button.OnClickAsObservable().Subscribe(_ =>
    //     {
    //         _isBgmPlaying = !_isBgmPlaying; 
    //         OnClickedBgmButton.OnNext((_stageId, _isBgmPlaying));
    //     }).AddTo(this);
    //     _bgmButton.SetBacklight(false);
    // }
}
