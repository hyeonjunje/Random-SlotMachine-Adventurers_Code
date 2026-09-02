using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Battle : UI_Base
{
    [Header ("턴 시작 UI")]
    [SerializeField] private TurnNotifyUI _turnNotifyUI;

    [Header ("마나 UI")]
    [SerializeField] private TMP_Text _textMana;

    [Header ("진행 UI")]
    [SerializeField] private SlotMachineSpinUI _slotMachineSpinUI;
    [SerializeField] private Button _buttonNext;

    [Header("토큰")]
    [SerializeField] private SlotMachineSkillTokenController _tokenController;

    [Header("타겟 지정")]
    [SerializeField] private TargetSelectUI _targetSelectUI;

    [Header("Card Preview")]
    [SerializeField] private SkillCardPreviewUI _skillCardPreviewPrefab;
    [SerializeField] private Transform _skillCardPreviewParent;

    private Dictionary<BattleAct, SkillCardPreviewUI> _activeCardPreviews = new Dictionary<BattleAct, SkillCardPreviewUI>();
    private Queue<SkillCardPreviewUI> _cardPreviewPool = new Queue<SkillCardPreviewUI>();

    private IDisposable _onChangedMana;

    private BingoResult _cachedBingoResultByDebugging = null;

    private void Update()
    {
        if(AppConfig.IsCheatEnabled && AppConfig.BootStrapperType == EBootstrapperType.Custom1)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                BattleAct battleAct = new BattleAct(_cachedBingoResultByDebugging.Owner, _cachedBingoResultByDebugging.Skill, true, _cachedBingoResultByDebugging.Bingo);
                _tokenController.CreateToken(battleAct);
            }
        }
    }
    

    public override void Open()
    {
        gameObject.SetActive (true);
        _targetSelectUI.HideImmediate();

        _onChangedMana = EventBus.Subscribe<StChangedManaEvent> (UpdateManaUI);
    }

    public override void Close()
    {
        gameObject.SetActive (false);

        _onChangedMana?.Dispose ();
    }

    public IEnumerator PlayStartBattleNotify()
    {
        _turnNotifyUI.gameObject.SetActive(true);
        yield return StartCoroutine(_turnNotifyUI.StartBattleNotify());
    }

    public IEnumerator PlayTurnNotify(int turn)
    {
        _turnNotifyUI.gameObject.SetActive (true);
        yield return StartCoroutine (_turnNotifyUI.PlayTurnNotify (turn));
    }

    bool tempFlag = false;
    public void SetNextButtonInteractable(bool flag)
    {
        tempFlag = flag;
        Invoke(nameof(SetNextButtonInteractableInvoke), 0.1f);
    }

    private void SetNextButtonInteractableInvoke()
    {
        _buttonNext.interactable = tempFlag;
    }

    public void SetActiveStartSlotMachineButton(bool flag)
    {
        if(flag)
        {
            _slotMachineSpinUI.Open();
        }
        else
        {
            _slotMachineSpinUI.Close();
        }
    }

    public void InitTokenController()
    {
        _tokenController.Init();
    }
    
    public IEnumerator CoSetTokenByResult(BingoResult[] bingoResults, bool isClear)
    {
        _cachedBingoResultByDebugging = bingoResults[0];

        // isClear시 적 행동도 다시 세팅해준다.
        if (isClear)
        {
            for (int i = 0; i < CharacterSystem.Instance.Enemies.Count; ++i)
            {
                EnemyView enemyView = CharacterSystem.Instance.Enemies[i];
                if (enemyView != null && enemyView.Enemy.IsDead == false)
                {
                    EnemyAI enemyAI = enemyView.Enemy.EnemyAI;
                    Skill skill = new Skill(enemyAI.CurrentAct, enemyView);
                    BattleAct battleAct = new BattleAct(enemyView, skill, false);

                    _tokenController.CreateToken(battleAct, enemyAI.ActCount);
                    yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.CreateTokenInterval);
                }
            }
        }

        foreach(BingoResult bingoResult in bingoResults)
        {
            if (bingoResult.MatchType != EKeywordMatchType.NonMatch)
            {
                BattleAct battleAct = new BattleAct(bingoResult.Owner, bingoResult.Skill, true, bingoResult.Bingo);
                int tokenMultiplier = ArtifactRuntimeState.EffectivePlayerTokenMultiplier;

                for (int i = 0; i < tokenMultiplier; i++)
                {
                    _tokenController.CreateToken(battleAct);
                    yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.CreateTokenInterval);
                }
            }
        }

        SetNextButtonInteractable(true);

        if (BattleSystem.Instance != null && BattleSystem.Instance.IsTutorialBattle)
        {
            UI_Tutorial tutorialUI = UIManager.Instance.Get<UI_Tutorial>(EUIType.UI_Tutorial);
            tutorialUI?.OnBattleTokensCreated();
        }
    }

    public void UpdateTargets()
    {
        if(BattleSystem.Instance.BattleState == EBattleState.SelectTarget)
        {
            _targetSelectUI.UpdateUI();

            // 모든 타겟이 선택되면 자동전투 실행
            if(BattleSystem.Instance.CurrentTargets.Count == CharacterSystem.Instance.Enemies.Count)
            {
                _targetSelectUI.Close();
                StartAutoBattle();
            }
        }
    }

    public IEnumerator CoUseToken()
    {
        yield return StartCoroutine(_tokenController.CoUseToken());
    }

    public IEnumerator CoDeleteToken(CharacterView owner)
    {
        yield return StartCoroutine(_tokenController.CoDeleteToken(owner));
    }

    public IEnumerator CoUseToken(BattleAct battleAct)
    {
        yield return StartCoroutine(_tokenController.CoUseToken(battleAct));
    }

    public void UpdateToken()
    {
        List<BattleAct> battleActs = new List<BattleAct>();
        foreach (ListItem_SlotMachineToken slotMachineToken in _tokenController.Tokens)
        {
            battleActs.Add(slotMachineToken.Item);
        }

        BattleSystem.Instance.UpdateActQueue(battleActs);
    }

    private void StartAutoBattle()
    {
        List<BattleAct> battleActs = new List<BattleAct>();
        foreach (ListItem_SlotMachineToken slotMachineToken in _tokenController.Tokens)
        {
            battleActs.Add(slotMachineToken.Item);
        }

        StartAutoBattleGA startAutoBattleGA = new StartAutoBattleGA(battleActs);
        ActionSystem.Instance.Perform(startAutoBattleGA);
    }

    private void UpdateManaUI(StChangedManaEvent changedManaEvent)
    {
        _textMana.text = $"{changedManaEvent.CurrentMana}/{changedManaEvent.MaxMana}";
    }

    #region UIEvent
    public void ShowCardPreview(BattleAct battleAct)
    {
        if (_activeCardPreviews.ContainsKey(battleAct))
        {
            return;
        }

        SkillCardPreviewUI preview;
        if (_cardPreviewPool.Count > 0)
        {
            preview = _cardPreviewPool.Dequeue();
        }
        else
        {
            preview = Instantiate(_skillCardPreviewPrefab, _skillCardPreviewParent);
        }

        _activeCardPreviews.Add(battleAct, preview);
        preview.ShowCardView(battleAct);
    }

    public void HideCardPreview(BattleAct battleAct)
    {
        if (_activeCardPreviews.TryGetValue(battleAct, out SkillCardPreviewUI preview))
        {
            _activeCardPreviews.Remove(battleAct);
            preview.HideCardView(() => 
            {
                _cardPreviewPool.Enqueue(preview);
            });
        }
    }

    public void OnClickNextButton()
    {
        bool isTargetRequired = false;

        foreach (ListItem_SlotMachineToken slotMachineToken in _tokenController.Tokens)
        {
            if(slotMachineToken.Item.Skill.IsTargetRequired)
            {
                isTargetRequired = true;
                break;
            }
        }

        UI_Tutorial tutorialUI = UIManager.Instance.Get<UI_Tutorial>(EUIType.UI_Tutorial);
        if (tutorialUI != null && tutorialUI.IsTutorialRunning && tutorialUI.ShouldBlockSlotConfirm)
        {
            return;
        }

        // 타겟을 지정할 필요있으면 타겟 지정 UI 켜준다.
        if(isTargetRequired)
        {
            if (tutorialUI != null && tutorialUI.IsTutorialRunning && tutorialUI.ShouldHandleSlotConfirm)
            {
                if (tutorialUI.TryHandleSlotConfirm(() => _targetSelectUI.Open()) == false)
                {
                    return;
                }
            }
            else
            {
                _targetSelectUI.Open();
            }
        }
        else // 없으면 바로 자동전투
        {
            StartAutoBattle();
        }

        SetNextButtonInteractable(false);
        UIManager.Instance.Close(EUIType.UI_SlotMachine);
    }

    public void OnClickPrevButton()
    {
        if(BattleSystem.Instance.BattleState == EBattleState.SelectTarget)
        {
            BattleSystem.Instance.ChangeBattleState(EBattleState.SlotMachine);
            BattleSystem.Instance.InitCurrentTarget();
            SetNextButtonInteractable(true);
            _targetSelectUI.Close();
            UIManager.Instance.Open(EUIType.UI_SlotMachine);
        }
    }

    public void OnStartSlotMachine()
    {
        if(ActionSystem.Instance.IsPerforming)
        {
            return;
        }

        UI_Tutorial tutorialUI = UIManager.Instance.Get<UI_Tutorial>(EUIType.UI_Tutorial);
        if (tutorialUI != null && tutorialUI.IsTutorialRunning && tutorialUI.ShouldBlockSlotMachineStart)
        {
            return;
        }

        if(BattleSystem.Instance.BattleState == EBattleState.StartTurn)
        {
            SetSlotMachineGA setSlotMachineGA = new SetSlotMachineGA();
            ActionSystem.Instance.Perform(setSlotMachineGA);

            SetActiveStartSlotMachineButton(false);
        }
    }
    #endregion
}
