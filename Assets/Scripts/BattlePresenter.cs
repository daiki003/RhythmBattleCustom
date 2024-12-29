using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using System;

public class BattlePresenter : MonoBehaviour
{
    [SerializeField] private BattleView _battleView;

    private string _stageId;
    private int _level;
    private StageMaster _currentStageMaster;

    private bool _isDuaringBattle;
    private int _criticalCount = 0;
    private int _hitCount = 0;
    private int _missCount = 0;
    private int _comboCount = 0;
    private int _maxComboCount = 0;

    public void Init(string stageId, int level)
    {
        _stageId = stageId;
        _level = level;
        _battleView.OnWhenClickedBack.Subscribe(_ =>
        {
            GameManager.instance.GoToTitle();
        }).AddTo(this);
        _battleView.DebugPanel.OnChangeMoveTime.Subscribe(time =>
        {
            MoveTime(time);
        }).AddTo(this);
        _battleView.OnCountUp.Subscribe(x =>
        {
            CountUp(x.Item1, x.Item2);
        }).AddTo(this);
        _battleView.Init(stageId);
        _currentStageMaster = MasterManager.GetStageMaster(_stageId);
        _battleView.CreateBalls(_currentStageMaster.notes[level], _currentStageMaster);
        BGMManager.instance.SetClip(_stageId);
    }

    public void StartBattle()
    {
        // 曲が始まる前にGC.Collect
        GC.Collect();
        _isDuaringBattle = true;
        BGMManager.instance.PlayFromIntro().Forget();
    }

    public void MoveTime(float time)
    {
        _battleView.Reset();
        _battleView.CreateBalls(_currentStageMaster.notes[_level], _currentStageMaster, time);
        BGMManager.instance.SetTime(time);
        BGMManager.instance.Play();
    }

    public async UniTask FinishBattle()
    {
        _maxComboCount = Math.Max(_comboCount, _maxComboCount);

        var clearState = new ClearState()
        {
            StageId = _stageId,
            Level = _level,
        };
        float totalCount = _criticalCount + _hitCount + _missCount;
        float criticalMultiple = 100f / totalCount;
        float hitMultiple = 50f / totalCount;
        float missMultiple = -100f / totalCount;
        float realScore = Mathf.Max(0, _criticalCount * criticalMultiple + _hitCount * hitMultiple + _missCount * missMultiple);
        if (clearState != null && clearState.Score <= realScore)
        {
            clearState.CriticalNumber = _criticalCount;
            clearState.HitNumber = _hitCount;
            clearState.MissNumber = _missCount;
            clearState.Score = realScore;
            clearState.Combo = _maxComboCount;
        }

        await UniTask.WaitForSeconds(2.5f);
        _battleView.StartResult(clearState, SaveDataManager.GetClearState(_stageId, _level), criticalMultiple, hitMultiple, missMultiple);
        BGMManager.instance.SetClip("Result", isLoop: true);
        BGMManager.instance.Play();
        if (!_battleView.IsTest)
        {
            SaveDataManager.UpdateClearState(clearState);
        }
    }

    private void CountUp(HitType hitType, int count = 1)
    {
        if (hitType == HitType.Hit)
        {
            _hitCount += count;
            _comboCount += count;
        }
        else if (hitType == HitType.Critical)
        {
            _criticalCount += count;
            _comboCount += count;
        }
        else
        {
            _missCount += count;
            _maxComboCount = Math.Max(_comboCount, _maxComboCount);
            _comboCount = 0;
        }
        _battleView.CountUpText(_criticalCount, _hitCount, _missCount, _comboCount);
    }

    void Update()
    {
        if (BGMManager.instance.IsFinishBgm && _isDuaringBattle)
        {
            _isDuaringBattle = false;
            FinishBattle().Forget();
        }
    }
}
