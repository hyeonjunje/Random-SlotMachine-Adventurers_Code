using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineViewer : UI_Base
{
    [Header("슬롯")]
    [SerializeField] private SlotMachineReel[] _reels;

    [Header("컴포넌트")]
    [SerializeField] private RectTransform _pivotSlotMachineBackground;
    [SerializeField] private Image _imageActiveIcon;

    [Header("Multi Spin Effects")]
    [SerializeField] private GameObject _particleGreatSuccess;
    [SerializeField] private GameObject _particleUltraSuccess;
    [SerializeField] private Transform _slotMachineTransform;
    [SerializeField] private float _slotMachineScaleMultiSpinTarget = 1.05f;
    [SerializeField] private float _slotMachineScaleMultiSpinDuration = 0.2f;

    [Header("플레이어 스킬")]
    [SerializeField] private SO_SkillData _rerollSkillData;
    [SerializeField] private SO_SkillData _cloneSkillData;
    [SerializeField] private SO_SkillData _exchangeSkillData;

    private Skill _rerollSkill;
    private Skill _cloneSkill;
    private Skill _exchangeSkill;

    private Coroutine _coSetTokenByResult = null;

    public override void Initialize()
    {
        base.Initialize();

        _rerollSkill = new Skill(_rerollSkillData, null);
        _cloneSkill = new Skill(_cloneSkillData, null);
        _exchangeSkill = new Skill(_exchangeSkillData, null);
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        RefreshTutorialActiveRows();

        if (Vector2.Distance(_pivotSlotMachineBackground.anchoredPosition, Vector2.zero) > Mathf.Epsilon)
        {
            OnClickActiveSlotMachine();
        }
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    public void SetSlotMachine(IReadOnlyList<EKeyword> keywords, EKeywordTypePos keywordTypePos)
    {
        RefreshTutorialActiveRows();

        for (int i = 0; i < _reels.Length; ++i)
        {
            if(i % SO_SlotMachineConfig.HORIZONTAL == (int)keywordTypePos)
            {
                SlotMachineReel reel = _reels[i];
                reel.SetReel(keywords, i);
            }
        }
    }

    // 일반 슬롯머신 결과
    public IEnumerator ShowResult(SlotMachineResult result, SO_SlotMachineConfig _config, bool isClear = true)
    {
        RefreshTutorialActiveRows();

        // 돌리기 전 초기화
        InitBeforeSpinSlotMachine(isClear);

        EKeyword[,] reelResult = result.reelResult;
        int lastActiveIndex = GetLastActiveReelIndex(reelResult);

        for (int y = 0; y < reelResult.GetLength(0); ++y)
        {
            for (int x = 0; x < reelResult.GetLength(1); ++x)
            {
                int index = x + y * reelResult.GetLength(0);
                if (IsActiveReel(index) == false)
                {
                    continue;
                }

                SlotMachineReel reel = _reels[index];
                reel.SetConfig(_config);
                reel.Spin(_config.SlotSpinDelay[index]);
            }
        }

        yield return new WaitForSeconds(_config.MoveDuration);

        // 릴 멈추기
        for (int y = 0; y < reelResult.GetLength(0); ++y)
        {
            for (int x = 0; x < reelResult.GetLength(1); ++x)
            {
                int index = x + y * reelResult.GetLength(0);
                if (IsActiveReel(index) == false)
                {
                    continue;
                }

                SlotMachineReel reel = _reels[index];

                // 마지막은 대기
                if (index == lastActiveIndex)
                {
                    yield return StartCoroutine(reel.CoStop(reelResult[y, x], _config.SlotStopDelay[index]));
                }
                else
                {
                    reel.Stop(reelResult[y, x], _config.SlotStopDelay[index]);
                }
            }
        }

        // 토큰 세팅
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        _coSetTokenByResult = StartCoroutine(uiBattle.CoSetTokenByResult(result.bingoResult, isClear));
        yield return _coSetTokenByResult;
    }

    // 일반 슬롯머신 특정 슬롯만 돌리기 결과
    public IEnumerator ShowResult(SlotMachineResult result, List<int> slotIndexes, SO_SlotMachineConfig _config, bool isClear = true)
    {
        RefreshTutorialActiveRows();
        slotIndexes = slotIndexes.Where(IsActiveReel).ToList();

        // 돌리기 전 초기화
        InitBeforeSpinSlotMachine(isClear);

        EKeyword[,] reelResult = result.reelResult;

        foreach(int slotIndex in slotIndexes)
        {
            SlotMachineReel reel = _reels[slotIndex];
            reel.SetConfig(_config);
            reel.Spin(0);
        }

        yield return new WaitForSeconds(_config.MoveDuration);

        // 릴 멈추기
        for(int i = 0; i < slotIndexes.Count; ++i)
        {
            int resultX = slotIndexes[i] % SO_SlotMachineConfig.HORIZONTAL;
            int resultY = slotIndexes[i] / SO_SlotMachineConfig.VERTICAL;

            SlotMachineReel reel = _reels[resultX + resultY * reelResult.GetLength(0)];

            // 마지막은 대기
            if (i == slotIndexes.Count - 1)
            {
                yield return StartCoroutine(reel.CoStop(reelResult[resultY, resultX], 0));
            }
            else
            {
                reel.Stop(reelResult[resultY, resultX], 0);
            }
        }

        // 토큰 세팅
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);

        _coSetTokenByResult = StartCoroutine(uiBattle.CoSetTokenByResult(result.bingoResult, isClear));
        yield return _coSetTokenByResult;
    }

    // 일반 슬롯머신 결과 즉시 표시
    public IEnumerator ShowResultImmediately(SlotMachineResult result, bool isClear = true)
    {
        RefreshTutorialActiveRows();

        // 돌리기 전 초기화
        InitBeforeSpinSlotMachine(isClear);

        // 릴 이름 변경
        EKeyword[,] reelResult = result.reelResult;
        for (int y = 0; y < reelResult.GetLength(0); ++y)
        {
            for (int x = 0; x < reelResult.GetLength(1); ++x)
            {
                int index = x + y * reelResult.GetLength(0);
                if (IsActiveReel(index) == false)
                {
                    continue;
                }

                SlotMachineReel reel = _reels[index];
                reel.SetCurrentSlotText(reelResult[y, x]);
            }
        }

        // 토큰 세팅
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);

        _coSetTokenByResult = StartCoroutine(uiBattle.CoSetTokenByResult(result.bingoResult, isClear));
        yield return _coSetTokenByResult;
    }

    public void PlayMultiSpinEffect(int spinIndex)
    {
        if (spinIndex == 1)
        {
            if (_particleGreatSuccess != null)
            {
                _particleGreatSuccess.SetActive(false);
                _particleGreatSuccess.SetActive(true);
            }
        }
        else if (spinIndex == 2)
        {
            if (_particleUltraSuccess != null)
            {
                _particleUltraSuccess.SetActive(false);
                _particleUltraSuccess.SetActive(true);
            }
        }

        if (_slotMachineTransform != null)
        {
            _slotMachineTransform.DOScale(_slotMachineScaleMultiSpinTarget, _slotMachineScaleMultiSpinDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }
    }

    public void PlayBingoHighlight(int bingoIndex)
    {
        // -1(적군 몬스터) 을 제외하고 빙고 완성 시 사운드
        if(bingoIndex != -1)
        {
            AudioManager.Instance.PlaySFX(ESfxId.SlotMachineComplete);
        }

        switch ((EBingo)bingoIndex)
        {
            case EBingo.Horizontal1:
                PlayHighlight(0);
                PlayHighlight(1);
                PlayHighlight(2);
                break;
            case EBingo.Horizontal2:
                PlayHighlight(3);
                PlayHighlight(4);
                PlayHighlight(5);
                break;
            case EBingo.Horizontal3:
                PlayHighlight(6);
                PlayHighlight(7);
                PlayHighlight(8);
                break;
            case EBingo.Vertical1:
                PlayHighlight(0);
                PlayHighlight(3);
                PlayHighlight(6);
                break;
            case EBingo.Vertical2:
                PlayHighlight(1);
                PlayHighlight(4);
                PlayHighlight(7);
                break;
            case EBingo.Vertical3:
                PlayHighlight(2);
                PlayHighlight(5);
                PlayHighlight(8);
                break;
            case EBingo.Diagonal1:
                PlayHighlight(0);
                PlayHighlight(4);
                PlayHighlight(8);
                break;
            case EBingo.Diagonal2:
                PlayHighlight(2);
                PlayHighlight(4);
                PlayHighlight(6);
                break;
        }
    }

    public void BlinkHighlight(List<int> slotIndexes)
    {
        StopBlinkHighlight();

        foreach(int slotIndex in slotIndexes)
        {
            if (IsActiveReel(slotIndex))
            {
                _reels[slotIndex].BlinkHighlight();
            }
        }
    }

    public void StopBlinkHighlight()
    {
        for (int i = 0; i < _reels.Length; ++i)
        {
            _reels[i].StopBlinkHighlight();
        }
    }

    private void RefreshTutorialActiveRows()
    {
        bool firstRowOnly =
            BattleSystem.Instance != null &&
            BattleSystem.Instance.IsTutorialBattle &&
            CharacterSystem.Instance != null &&
            CharacterSystem.Instance.Players.Count == 1;

        for (int i = 0; i < _reels.Length; ++i)
        {
            bool active = firstRowOnly == false || i < SO_SlotMachineConfig.HORIZONTAL;
            if (_reels[i].gameObject.activeSelf != active)
            {
                _reels[i].gameObject.SetActive(active);
            }
        }
    }

    private bool IsActiveReel(int index)
    {
        return index >= 0 &&
            index < _reels.Length &&
            _reels[index] != null &&
            _reels[index].gameObject.activeSelf;
    }

    private int GetLastActiveReelIndex(EKeyword[,] reelResult)
    {
        int lastActiveIndex = -1;
        for (int y = 0; y < reelResult.GetLength(0); ++y)
        {
            for (int x = 0; x < reelResult.GetLength(1); ++x)
            {
                int index = x + y * reelResult.GetLength(0);
                if (IsActiveReel(index))
                {
                    lastActiveIndex = index;
                }
            }
        }

        return lastActiveIndex;
    }

    private void PlayHighlight(int index)
    {
        if (IsActiveReel(index))
        {
            _reels[index].PlayHighlight();
        }
    }

    private void InitBeforeSpinSlotMachine(bool clear)
    {
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.SetNextButtonInteractable(false);

        if(clear)
        {
            if (_particleGreatSuccess != null) _particleGreatSuccess.SetActive(false);
            if (_particleUltraSuccess != null) _particleUltraSuccess.SetActive(false);

            // 생성되고 있는 코루틴 중지
            if (_coSetTokenByResult != null)
            {
                StopCoroutine(_coSetTokenByResult);
            }

            uiBattle.InitTokenController();
        }
    }

    private void TryExcutePlayerSkill(Skill skill)
    {
        if (ManaSystem.Instance.CanSpend(skill.ManaCost))
        {
            if (skill.TotalEffect[0] is ISelectionResolver selectionResolver)
            {
                UI_SelectionContext uiSelectionContext = UIManager.Instance.Get<UI_SelectionContext>(EUIType.UI_SelectionContext);
                uiSelectionContext.ResolveSelection(selectionResolver.SelectionResolver, () =>
                {
                    SpendManaGA spendManaGA = new SpendManaGA(skill.ManaCost);
                    ActionSystem.Instance.Perform(spendManaGA, () =>
                    {
                        PerformEffectGA performEffectGA = new PerformEffectGA(skill.TotalEffect[0]);
                        ActionSystem.Instance.Perform(performEffectGA);
                    });
                });
            }
        }
        else
        {
            ManaSystem.Instance.ShowManaShortagegMessage();
        }
    }

    #region UIEvent
    public void OnClickSpinSlotMachine()
    {
        if(ActionSystem.Instance.IsPerforming)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(ESfxId.SlotMachineReroll); // 리롤 사운드.
        SlotMachineSystem.Instance.TrySpin (1);
    }

    public void OnClickActiveSlotMachine()
    {
        // 비활성화하면 코루틴이 중단돼서 이동으로 바꿈 (코드 구조가 코루틴 기반이라 코루틴말고 다른걸 사용못함)
        if (Vector2.Distance(_pivotSlotMachineBackground.anchoredPosition, Vector2.zero) < Mathf.Epsilon)
        {
            _pivotSlotMachineBackground.anchoredPosition = Vector2.down * 3000f;
            _imageActiveIcon.sprite = SpriteManager.Instance.GetSprite("function_icon_eye_hide");
        }
        else
        {
            _pivotSlotMachineBackground.anchoredPosition = Vector2.zero;
            _imageActiveIcon.sprite = SpriteManager.Instance.GetSprite("function_icon_eye_show");
        }
    }

    public void OnClickPlayerSkillKeywordReroll()
    {
        TryExcutePlayerSkill(_rerollSkill);
    }

    public void OnClickPlayerSkillKeywordClone()
    {
        TryExcutePlayerSkill(_cloneSkill);
    }

    public void OnClickPlayerSkillKeywordExchange()
    {
        TryExcutePlayerSkill(_exchangeSkill);
    }
    #endregion
}
