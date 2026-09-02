using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotMachineSystem : SingletonScene<SlotMachineSystem>
{
    [SerializeField] private SO_SlotMachineConfig _slotMachineConfig;

    private SlotMachineEngine _engine;
    private SlotMachineResult _slotMachineResult;
    private EKeyword[,] _preRerollSnapshotKeywords;
    private readonly HashSet<int> _rerolledSlotIndexesThisTurn = new HashSet<int>();
    private bool _hasPerformedInitialSpinThisTurn;
    private int _battleRerollCount;

    public int FreeRerollCount { get; private set; } = 0;
    public int CurrentTurnRerollCount { get; private set; } = 0;
    public int BattleRerollCount => _battleRerollCount;

    protected override void Awake()
    {
        _engine = new SlotMachineEngine(_slotMachineConfig);

        // ?щ’癒몄떊 ?숈옉
        ActionSystem.AttachPerformer<SetSlotMachineGA>(SetSlotMachinePerformer);
        ActionSystem.AttachPerformer<SpinSlotMachineGA>(SpinSlotMachinePerformer);

        // ?щ’癒몄떊 議곗옉
        ActionSystem.AttachPerformer<ChangeSlotMachineKeywordGA>(ChangeSlotMachineKeywordPerformer);
        ActionSystem.AttachPerformer<RerollSlotMachineKeywordGA>(RerollSlotMachineKeywordPerformer);
        ActionSystem.AttachPerformer<AddFreeRerollGA> (AddFreeRerollPerformer);
        ActionSystem.AttachPerformer<RerollSlotMachineKeywordAddTokenGA>(RerollSlotMachineKeywordAddTokenPerformer);
        ActionSystem.AttachPerformer<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(RerollSlotMachineKeywordAddTokenInBattlePhasePerformer);
        ActionSystem.AttachPerformer<RerollSlotMachineLineGA>(RerollSlotMachineLinePerformer);

        // ?щ’癒몄떊 ?ㅼ썙??異붽?
        ActionSystem.AttachPerformer<AddSlotMachineKeywordGA>(AddSlotMachineKeywordPerformer);
        ActionSystem.AttachPerformer<AddSlotMachineTempKeywordGA>(AddSlotMachineTempKeywordPerformer);

        // ?щ’癒몄떊 ?좏겙 ?ъ슜
        ActionSystem.AttachPerformer<ClickUseSlotMachineTokenGA>(ClickUseSlotMachineTokenPerformer);
        ActionSystem.AttachPerformer<BlockedRerollGA>(BlockedRerollPerformer);

        ActionSystem.SubscribeReaction<ClearNodeGA>(SubscribeClearNodeGA, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<StartBattleGA> (OnStartBattle, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<StartTurnGA> (OnStartTurn, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<ClickUseSlotMachineTokenGA>(SubscribeClickUseSlotMachineTokenGA, EReactionTiming.Post);

        ActionSystem.AttachPerformer<RemoveSlotMachineKeywordGA> (RemoveKeywordPerformer);

        ActionSystem.AttachPerformer<LevelUpKeywordGA> (LevelUpKeywordPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<SetSlotMachineGA>();
        ActionSystem.DetachPerformer<SpinSlotMachineGA>();

        // ?щ’癒몄떊 議곗옉
        ActionSystem.DetachPerformer<ChangeSlotMachineKeywordGA>();
        ActionSystem.DetachPerformer<RerollSlotMachineKeywordGA>();
        ActionSystem.DetachPerformer<AddFreeRerollGA>();
        ActionSystem.DetachPerformer<RerollSlotMachineKeywordAddTokenGA>();
        ActionSystem.DetachPerformer<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>();
        ActionSystem.DetachPerformer<RerollSlotMachineLineGA>();

        // ?щ’癒몄떊 ?ㅼ썙??異붽?
        ActionSystem.DetachPerformer<AddSlotMachineKeywordGA>();
        ActionSystem.DetachPerformer<AddSlotMachineTempKeywordGA>();

        ActionSystem.DetachPerformer<ClickUseSlotMachineTokenGA>();
        ActionSystem.DetachPerformer<BlockedRerollGA>();

        ActionSystem.UnSubscribeReaction<ClearNodeGA>(SubscribeClearNodeGA, EReactionTiming.Pre);
        ActionSystem.UnSubscribeReaction<StartBattleGA>(OnStartBattle, EReactionTiming.Pre);
        ActionSystem.UnSubscribeReaction<StartTurnGA>(OnStartTurn, EReactionTiming.Pre);
        ActionSystem.UnSubscribeReaction<ClickUseSlotMachineTokenGA>(SubscribeClickUseSlotMachineTokenGA, EReactionTiming.Post);

        // ?щ’癒몄떊 ?ㅼ썙???쒓굅
        ActionSystem.DetachPerformer<RemoveSlotMachineKeywordGA> ();

        ActionSystem.DetachPerformer<LevelUpKeywordGA> ();

    }
    private void OnStartBattle(StartBattleGA startBattleGA)
    {
        FreeRerollCount = 0;
        _battleRerollCount = 0;
        CurrentTurnRerollCount = 0;
        _hasPerformedInitialSpinThisTurn = false;
        _preRerollSnapshotKeywords = null;
        _rerolledSlotIndexesThisTurn.Clear();
        ArtifactRuntimeState.FirstTurnTemporaryFreeRerolls = 0;
        ArtifactRuntimeState.ResetBattleScopedState();
    }

    private void OnStartTurn(StartTurnGA startTurnGA)
    {
        CurrentTurnRerollCount = 0;
        _rerolledSlotIndexesThisTurn.Clear();
    }
    private IEnumerator AddFreeRerollPerformer(AddFreeRerollGA addFreeRerollGA)
    {
        FreeRerollCount += addFreeRerollGA.Amount;
        EventBus.Publish (new StSendMessageEvent (LocalizationManager.Instance.Get("CS_SLOTMACHINESYSTEM_016"), EMessageType.Notice));

        yield return null;
    }

    IEnumerator SetSlotMachinePerformer(SetSlotMachineGA setSlotMachineGA)
    {
        BattleSystem.Instance.ChangeBattleState(EBattleState.SlotMachine);

        UIManager.Instance.Open(EUIType.UI_SlotMachine);
        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);

        _engine.ClearPool();

        // 二쇱뼱 ?ㅼ썙???щ’癒몄떊???ｊ린
        foreach(EKeyword subjectKeyword in DataManager.Instance.GameModel.SubjectKeywords)
        {
            _engine.AddKeyword(subjectKeyword, EKeywordType.Subject);
        }

        // 遺???ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword adverbKeyword in DataManager.Instance.GameModel.AdverbKeywords)
        {
            _engine.AddKeyword(adverbKeyword, EKeywordType.Adverb);
        }

        // ?숈궗 ?ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword verbKeyword in DataManager.Instance.GameModel.VerbKeywords)
        {
            _engine.AddKeyword(verbKeyword, EKeywordType.Verb);
        }

        // ?二??ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword verbKeyword in DataManager.Instance.GameModel.CurseKeywords)
        {
            _engine.AddKeyword(verbKeyword, EKeywordType.Curse);
        }

        // ?꾩떆 二쇱뼱 ?ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword subjectKeyword in DataManager.Instance.GameModel.TempSubjectKeywords)
        {
            _engine.AddKeyword(subjectKeyword, EKeywordType.Subject);
        }

        // ?꾩떆 遺???ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword adverbKeyword in DataManager.Instance.GameModel.TempAdverbKeywords)
        {
            _engine.AddKeyword(adverbKeyword, EKeywordType.Adverb);
        }

        // ?꾩떆 ?숈궗 ?ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword verbKeyword in DataManager.Instance.GameModel.TempVerbKeywords)
        {
            _engine.AddKeyword(verbKeyword, EKeywordType.Verb);
        }

        // ?꾩떆 ?二??ㅼ썙???щ’癒몄떊???ｊ린
        foreach (EKeyword verbKeyword in DataManager.Instance.GameModel.TempCurseKeywords)
        {
            _engine.AddKeyword(verbKeyword, EKeywordType.Curse);
        }

        slotMachineViewer.SetSlotMachine(_engine.SubjectPools, EKeywordTypePos.Subject);
        slotMachineViewer.SetSlotMachine(_engine.AdverbPools, EKeywordTypePos.Adverb);
        slotMachineViewer.SetSlotMachine(_engine.VerbPools, EKeywordTypePos.Verb);
        _hasPerformedInitialSpinThisTurn = false;
        _preRerollSnapshotKeywords = null;
        _rerolledSlotIndexesThisTurn.Clear();

        yield return null;

        SpinSlotMachineGA spinSlotMachineGA = new SpinSlotMachineGA(GetRandomSuccessType());
        ActionSystem.Instance.AddReaction(spinSlotMachineGA);
    }

    IEnumerator SpinSlotMachinePerformer(SpinSlotMachineGA spinSlotMachineGA)
    {
        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        bool isReroll = _hasPerformedInitialSpinThisTurn;

        if (isReroll)
        {
            if (ArtifactRuntimeState.IsRerollDisabled)
            {
                yield break;
            }

            CapturePreRerollSnapshot();
        }

        ESlotMachineSuccessType successType = GetResolvedSuccessType(spinSlotMachineGA.SuccessType);
        bool forceFail = successType == ESlotMachineSuccessType.Fail;
        int spinCount = 1;
        switch (successType)
        {
            case ESlotMachineSuccessType.Fail: spinCount = 1; break;
            case ESlotMachineSuccessType.Success: spinCount = 1; break;
            case ESlotMachineSuccessType.GreatSuccess: spinCount = 2; break;
            case ESlotMachineSuccessType.UltraSuccess: spinCount = 3; break;
        }

        for (int i = 0; i < spinCount; i++)
        {
            // ?щ’癒몄떊 ?뚮━湲?
            _slotMachineResult = _engine.PickOne(spinSlotMachineGA.HigherTierWeightMultiplier);
            ApplyTutorialSlotRestriction();

            if (forceFail)
            {
                _slotMachineResult.bingoResult = CreateFailedBingoResults();
            }

            if (isReroll && i == 0)
            {
                FinalizeReroll(GetAllSlotIndexes(), false);
            }

            bool isClear = (i == 0);

            if (i > 0)
            {
                slotMachineViewer.PlayMultiSpinEffect(i);
            }

            // ?щ’癒몄떊 UI??蹂댁뿬二쇨린 
            yield return StartCoroutine(slotMachineViewer.ShowResult(_slotMachineResult, _slotMachineConfig, isClear));

            // 파티클 등 연출이 끝날 때까지 잠시 대기
            if (i < spinCount - 1)
            {
                yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.MultiSpinDelayTime);
            }
        }

        _hasPerformedInitialSpinThisTurn = true;
    }

    private ESlotMachineSuccessType GetResolvedSuccessType(ESlotMachineSuccessType successType)
    {
        if (BattleSystem.Instance != null && BattleSystem.Instance.IsTutorialBattle)
        {
            return ESlotMachineSuccessType.Success;
        }

        return successType;
    }

    private void ApplyTutorialSlotRestriction()
    {
        if (BattleSystem.Instance == null ||
            BattleSystem.Instance.IsTutorialBattle == false ||
            CharacterSystem.Instance == null ||
            _slotMachineResult?.reelResult == null)
        {
            return;
        }

        EKeyword adverb = GetFirstKeyword(_engine.AdverbPools);
        EKeyword verb = GetFirstKeyword(_engine.VerbPools);
        if (adverb == EKeyword.None || verb == EKeyword.None)
        {
            return;
        }

        int playerCount = CharacterSystem.Instance.Players.Count;
        if (playerCount == 1)
        {
            EKeyword subject = CharacterSystem.Instance.Players[0].Player.PlayerData.SubjectKeyword;

            SetSlotMachineRow(0, subject, adverb, verb);
            SetSlotMachineRow(1, subject, adverb, adverb);
            SetSlotMachineRow(2, subject, verb, verb);

            _slotMachineResult.bingoResult = _engine.Judge();
            KeepOnlyBingo(EBingo.Horizontal1);
            return;
        }

        for (int row = 0; row < SO_SlotMachineConfig.VERTICAL; row++)
        {
            PlayerView playerView = GetTutorialPlayerForRow(row);
            if (playerView != null)
            {
                EKeyword subject = playerView.Player.PlayerData.SubjectKeyword;
                SetSlotMachineRow(row, subject, adverb, verb);
            }
            else
            {
                SetSlotMachineRow(row, GetFirstKeyword(_engine.SubjectPools), adverb, adverb);
            }
        }

        _slotMachineResult.bingoResult = _engine.Judge();
        KeepOnlyTutorialHorizontalBingos();
    }

    private void KeepOnlyBingo(EBingo allowedBingo)
    {
        if (_slotMachineResult?.bingoResult == null)
        {
            return;
        }

        for (int i = 0; i < _slotMachineResult.bingoResult.Length; i++)
        {
            EBingo bingo = (EBingo)i;
            if (bingo == allowedBingo)
            {
                continue;
            }

            _slotMachineResult.bingoResult[i] = new BingoResult(EKeywordMatchType.NonMatch, null, null, bingo);
        }
    }

    private void SetSlotMachineRow(int row, EKeyword subject, EKeyword adverb, EKeyword verb)
    {
        _slotMachineResult.reelResult[row, (int)EKeywordTypePos.Subject] = subject;
        _slotMachineResult.reelResult[row, (int)EKeywordTypePos.Adverb] = adverb;
        _slotMachineResult.reelResult[row, (int)EKeywordTypePos.Verb] = verb;
    }

    private void KeepOnlyTutorialHorizontalBingos()
    {
        if (_slotMachineResult?.bingoResult == null)
        {
            return;
        }

        for (int i = 0; i < _slotMachineResult.bingoResult.Length; i++)
        {
            EBingo bingo = (EBingo)i;
            bool isAllowed =
                bingo == EBingo.Horizontal1 && GetTutorialPlayerForRow(0) != null ||
                bingo == EBingo.Horizontal2 && GetTutorialPlayerForRow(1) != null ||
                bingo == EBingo.Horizontal3 && GetTutorialPlayerForRow(2) != null;

            if (isAllowed)
            {
                continue;
            }

            _slotMachineResult.bingoResult[i] = new BingoResult(EKeywordMatchType.NonMatch, null, null, bingo);
        }
    }

    private PlayerView GetTutorialPlayerForRow(int row)
    {
        int playerIndex = row switch
        {
            0 => 1,
            1 => 0,
            2 => 2,
            _ => row,
        };

        if (playerIndex < 0 || playerIndex >= CharacterSystem.Instance.Players.Count)
        {
            return null;
        }

        return CharacterSystem.Instance.Players[playerIndex];
    }

    private EKeyword GetFirstKeyword(IReadOnlyList<EKeyword> keywords)
    {
        if (keywords == null || keywords.Count == 0)
        {
            return EKeyword.None;
        }

        return keywords[0];
    }

    private BingoResult[] CreateFailedBingoResults()
    {
        BingoResult[] results = new BingoResult[(int)EBingo.Size];
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = new BingoResult(EKeywordMatchType.NonMatch, null, null, EBingo.Horizontal1);
        }

        return results;
    }

    IEnumerator ChangeSlotMachineKeywordPerformer(ChangeSlotMachineKeywordGA changeSlotMachineKeywordGA)
    {
        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);

        for(int i = 0; i < changeSlotMachineKeywordGA.SlotMachineKeywords.Count; ++i)
        {
            int slotIndex = changeSlotMachineKeywordGA.SlotIndexes[i];
            int x = slotIndex % SO_SlotMachineConfig.HORIZONTAL;
            int y = slotIndex / SO_SlotMachineConfig.VERTICAL;
            _slotMachineResult.reelResult[y, x] = changeSlotMachineKeywordGA.SlotMachineKeywords[i];
        }

        // ?щ’癒몄떊 ?뚮━湲?
        _slotMachineResult.bingoResult = _engine.Judge();

        // ?щ’癒몄떊 UI??蹂댁뿬二쇨린 
        yield return StartCoroutine(slotMachineViewer.ShowResultImmediately(_slotMachineResult));
    }

    IEnumerator RerollSlotMachineKeywordPerformer(RerollSlotMachineKeywordGA rerollSlotMachineKeywordGA)
    {
        if (ArtifactRuntimeState.IsRerollDisabled || _slotMachineResult == null)
        {
            yield break;
        }

        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        int slotIndex = rerollSlotMachineKeywordGA.SlotIndex;
        int x = slotIndex % SO_SlotMachineConfig.HORIZONTAL;
        int y = slotIndex / SO_SlotMachineConfig.VERTICAL;

        CapturePreRerollSnapshot();

        // ?щ’癒몄떊 ???щ’留??뚮━湲?
        _slotMachineResult.reelResult[y, x] = _engine.GetKeywordsByX((EKeywordTypePos)x).GetRandomElement();
        FinalizeReroll(new List<int> { slotIndex }, true);

        // ?щ’癒몄떊 UI??蹂댁뿬二쇨린 
        yield return StartCoroutine(slotMachineViewer.ShowResult(_slotMachineResult, new List<int> { slotIndex }, _slotMachineConfig));
    }

    private IEnumerator RerollSlotMachineKeywordAddTokenPerformer(RerollSlotMachineKeywordAddTokenGA rerollSlotMachineKeywordAddTokenGA)
    {
        if (ArtifactRuntimeState.IsRerollDisabled || _slotMachineResult == null)
        {
            yield break;
        }

        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        CapturePreRerollSnapshot();

        if (rerollSlotMachineKeywordAddTokenGA.SlotMachineRerollKeywordType == ESlotMachineRerollKeywordType.Cross)
        {
            int x = -1, y = -1;

            foreach(Keyword clickedKeyword in DataManager.Instance.GameModel.ClickedKeywords)
            {
                if(clickedKeyword.KeywordData.Keyword == rerollSlotMachineKeywordAddTokenGA.CausedKeyword)
                {
                    x = clickedKeyword.SlotIndex % SO_SlotMachineConfig.HORIZONTAL;
                    y = clickedKeyword.SlotIndex / SO_SlotMachineConfig.VERTICAL;
                }
            }

            if(x == -1 || y == -1)
            {
                yield break;
            }

            HashSet<int> slotIndexes = new HashSet<int>();
            // ?뱀꺼??怨?湲곗? ??옄濡?
            for (int i = 0; i < SO_SlotMachineConfig.VERTICAL; ++i)
            {
                _slotMachineResult.reelResult[i, x] = _engine.GetKeywordsByX((EKeywordTypePos)x).GetRandomElement();
                slotIndexes.Add(x + i * SO_SlotMachineConfig.VERTICAL);
            }
            for (int i = 0; i < SO_SlotMachineConfig.HORIZONTAL; ++i)
            {
                _slotMachineResult.reelResult[y, i] = _engine.GetKeywordsByX((EKeywordTypePos)i).GetRandomElement();
                slotIndexes.Add(i + y * SO_SlotMachineConfig.VERTICAL);
            }

            FinalizeReroll(slotIndexes.ToList(), true);

            yield return StartCoroutine(slotMachineViewer.ShowResult(_slotMachineResult, slotIndexes.ToList(), _slotMachineConfig, false));
        }
        else
        {
            // ?щ’癒몄떊 ?꾩껜 ?뚮━湲?
            _slotMachineResult = _engine.PickOne();
            FinalizeReroll(GetAllSlotIndexes(), true);

            // ?щ’癒몄떊 UI??蹂댁뿬二쇨린 
            yield return StartCoroutine(slotMachineViewer.ShowResult(_slotMachineResult, _slotMachineConfig, false));
        }

        yield return null;
    }

    private IEnumerator RerollSlotMachineKeywordAddTokenInBattlePhasePerformer(RerollSlotMachineKeywordAddTokenInBattlePhaseGA rerollSlotMachineKeywordAddTokenInBattlePhaseGA)
    {
        if (ArtifactRuntimeState.IsRerollDisabled)
        {
            yield break;
        }

        // ?곸씠 ?섎굹諛뽰뿉 ?녿뒗??hp媛 0?대씪硫?return 
        if(CharacterSystem.Instance.Enemies.Count == 1 && CharacterSystem.Instance.Enemies[0].Character.HealthController.CurrentHp <= 0)
        {
            yield break;
        }
        
        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        slotMachineViewer.Open();

        // ?щ’癒몄떊 ?꾩껜 ?뚮━湲?
        CapturePreRerollSnapshot();
        _slotMachineResult = _engine.PickOne();
        FinalizeReroll(GetAllSlotIndexes(), true);

        // ?щ’癒몄떊 UI??蹂댁뿬二쇨린 
        yield return StartCoroutine(slotMachineViewer.ShowResult(_slotMachineResult, _slotMachineConfig, false));

        slotMachineViewer.Close();

        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.UpdateToken();
    }

    private IEnumerator RerollSlotMachineLinePerformer(RerollSlotMachineLineGA rerollSlotMachineLineGA)
    {
        if (_slotMachineResult == null || ArtifactRuntimeState.IsRerollDisabled)
        {
            yield break;
        }

        SlotMachineViewer slotMachineViewer = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        bool wasOpen = slotMachineViewer != null && slotMachineViewer.gameObject.activeSelf;
        if (slotMachineViewer != null && !wasOpen)
        {
            slotMachineViewer.Open();
        }

        CapturePreRerollSnapshot();
        HashSet<int> rerollIndexes = new HashSet<int>();

        if (rerollSlotMachineLineGA.Direction == ESlotMachineLineDirection.Horizontal)
        {
            List<int> candidateRows = new List<int>();
            for (int y = 0; y < SO_SlotMachineConfig.VERTICAL; y++)
            {
                candidateRows.Add(y);
            }

            candidateRows.Shuffle();
            int count = UnityEngine.Mathf.Min(rerollSlotMachineLineGA.LineCount, candidateRows.Count);
            for (int i = 0; i < count; i++)
            {
                int y = candidateRows[i];
                for (int x = 0; x < SO_SlotMachineConfig.HORIZONTAL; x++)
                {
                    _slotMachineResult.reelResult[y, x] = _engine.GetKeywordsByX((EKeywordTypePos)x).GetRandomElement();
                    rerollIndexes.Add(x + y * SO_SlotMachineConfig.HORIZONTAL);
                }
            }
        }
        else
        {
            List<int> candidateColumns = new List<int>();
            for (int x = 0; x < SO_SlotMachineConfig.HORIZONTAL; x++)
            {
                candidateColumns.Add(x);
            }

            candidateColumns.Shuffle();
            int count = UnityEngine.Mathf.Min(rerollSlotMachineLineGA.LineCount, candidateColumns.Count);
            for (int i = 0; i < count; i++)
            {
                int x = candidateColumns[i];
                for (int y = 0; y < SO_SlotMachineConfig.VERTICAL; y++)
                {
                    _slotMachineResult.reelResult[y, x] = _engine.GetKeywordsByX((EKeywordTypePos)x).GetRandomElement();
                    rerollIndexes.Add(x + y * SO_SlotMachineConfig.HORIZONTAL);
                }
            }
        }

        FinalizeReroll(rerollIndexes.ToList(), false);

        if (slotMachineViewer != null)
        {
            yield return StartCoroutine(slotMachineViewer.ShowResult(_slotMachineResult, rerollIndexes.ToList(), _slotMachineConfig, false));

            if (!wasOpen)
            {
                slotMachineViewer.Close();
            }
        }

        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.UpdateToken();
    }

    private IEnumerator AddSlotMachineKeywordPerformer(AddSlotMachineKeywordGA addSlotMachineKeywordGA)
    {
        if (addSlotMachineKeywordGA.Cost > 0)
        {
            if (!UIHudSystem.Instance.CanPayGold (addSlotMachineKeywordGA.Cost))
            {
                EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_SLOTMACHINESYSTEM_017"), EMessageType.Warning));
                yield break;
            }
        }

        EKeyword keyword = addSlotMachineKeywordGA.Keyword;
        SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(keyword);

        if (keywordData == null)
        {
            yield break;
        }

        DataManager.Instance.GameModel.GainedKeyword++;

        switch (keywordData.KeywordType)
        {
            case EKeywordType.Subject:
                DataManager.Instance.GameModel.SubjectKeywords.Add(keyword);
                break;
            case EKeywordType.Adverb:
                DataManager.Instance.GameModel.AdverbKeywords.Add(keyword);
                break;
            case EKeywordType.Verb:
                DataManager.Instance.GameModel.VerbKeywords.Add(keyword);
                break;
            case EKeywordType.Curse:
                DataManager.Instance.GameModel.CurseKeywords.Add(keyword);
                break;
        }

        TryTriggerGrowthPotionLevelUp();
        yield return null;
    }

    private IEnumerator AddSlotMachineTempKeywordPerformer(AddSlotMachineTempKeywordGA addSlotMachineTempKeywordGA)
    {
        EKeyword keyword = addSlotMachineTempKeywordGA.Keyword;
        SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(keyword);

        if(keywordData == null)
        {
            yield break;
        }

        switch (keywordData.KeywordType)
        {
            case EKeywordType.Subject:
                DataManager.Instance.GameModel.TempSubjectKeywords.Add(keyword);
                break;
            case EKeywordType.Adverb:
                DataManager.Instance.GameModel.TempAdverbKeywords.Add(keyword);
                break;
            case EKeywordType.Verb:
                DataManager.Instance.GameModel.TempVerbKeywords.Add(keyword);
                break;
            case EKeywordType.Curse:
                DataManager.Instance.GameModel.TempCurseKeywords.Add(keyword);
                break;
        }

        yield return null;
    }

    private IEnumerator ClickUseSlotMachineTokenPerformer(ClickUseSlotMachineTokenGA clickUseSlotMachineTokenGA)
    {
        yield return null;
        Skill skill = clickUseSlotMachineTokenGA.BattleAct.Skill;

        if (skill.IsClickableSkill)
        {
            if (ArtifactRuntimeState.IsRerollDisabled && skill.ClickEffect.Exists(IsRerollEffect))
            {
                ShowRerollDisabledMessage();
                yield break;
            }

            foreach(Keyword keyword in clickUseSlotMachineTokenGA.BattleAct.Skill.ClickableKeywords)
            {
                DataManager.Instance.GameModel.ClickedKeywords.Add(keyword);
            }

            foreach(Effect effect in skill.ClickEffect)
            {
                PerformEffectGA performEffectGA = new PerformEffectGA(effect);
                ActionSystem.Instance.AddReaction(performEffectGA);
            }

            // ?ъ슜 ???좏겙 ?쒓굅
            UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
            yield return StartCoroutine(uiBattle.CoUseToken(clickUseSlotMachineTokenGA.BattleAct));
        }
    }

    private IEnumerator BlockedRerollPerformer(BlockedRerollGA blockedRerollGA)
    {
        if (blockedRerollGA.RefundMana > 0)
        {
            ActionSystem.Instance.AddReaction(new FillManaGA(blockedRerollGA.RefundMana));
        }

        ShowRerollDisabledMessage();
        yield return null;
    }

    private ESlotMachineSuccessType GetRandomSuccessType()
    {
        float random = UnityEngine.Random.value;
        var model = DataManager.Instance.GameModel;

        float failureProbability = Mathf.Clamp01(model.FailureProbability);
        float successProbability = Mathf.Clamp01(model.SuccessProbability);
        float greatSuccessProbability = Mathf.Clamp01(model.GreatSuccessProbability * ArtifactRuntimeState.GreatSuccessProbabilityMultiplier);

        if (random <= failureProbability)
        {
            return ESlotMachineSuccessType.Fail;
        }

        random -= failureProbability;
        if (random <= successProbability)
        {
            return ESlotMachineSuccessType.Success;
        }

        if (random <= successProbability + greatSuccessProbability)
        {
            return ESlotMachineSuccessType.GreatSuccess;
        }
        
        return ESlotMachineSuccessType.UltraSuccess;
    }

    public void TrySpin(int manaCost)
    {
        bool isReroll = _hasPerformedInitialSpinThisTurn;
        if (isReroll && ArtifactRuntimeState.IsRerollDisabled)
        {
            ShowRerollDisabledMessage();
            return;
        }

        if (isReroll &&
            BattleSystem.Instance != null &&
            BattleSystem.Instance.CurrentTurn == 1 &&
            ArtifactRuntimeState.TryConsumeFirstTurnTemporaryFreeReroll())
        {
            ActionSystem.Instance.Perform(new SpinSlotMachineGA(GetRandomSuccessType()));
            return;
        }

        // 臾대즺 由щ· 泥섎━
        if (FreeRerollCount > 0)
        {
            FreeRerollCount--;
            ActionSystem.Instance.Perform (new SpinSlotMachineGA (GetRandomSuccessType()));
            return;
        }

        // 留덈굹 ?ъ슜 泥섎━
        if (ManaSystem.Instance.CanSpend (manaCost))
        {
            ActionSystem.Instance.Perform (new SpendManaGA (manaCost), () =>
            {
                if (isReroll)
                {
                    ArtifactRuntimeState.MarkNextSpinAsManaSpentReroll();
                }
                ActionSystem.Instance.Perform (new SpinSlotMachineGA (GetRandomSuccessType()));
            });
        }
        else
        {
            ManaSystem.Instance.ShowManaShortagegMessage ();
        }
    }

    private void SubscribeClearNodeGA(ClearNodeGA clearNodeGA)
    {
        DataManager.Instance.GameModel.TempSubjectKeywords.Clear();
        DataManager.Instance.GameModel.TempAdverbKeywords.Clear();
        DataManager.Instance.GameModel.TempVerbKeywords.Clear();
        DataManager.Instance.GameModel.TempCurseKeywords.Clear();
    }

    // slotIndex (0 ~ 8)??諛쏆쑝硫??대떦 ?щ’??寃곌낵瑜?諛섑솚?섎뒗 硫붿냼??
    public EKeyword GetSlotMachineResultKeyword(int slotIndex)
    {
        if(_slotMachineResult != null)
        {
            int x = slotIndex % SO_SlotMachineConfig.HORIZONTAL;
            int y = slotIndex / SO_SlotMachineConfig.VERTICAL;
            return _slotMachineResult.reelResult[y, x];
        }
        return EKeyword.None;
    }

    private void SubscribeClickUseSlotMachineTokenGA(ClickUseSlotMachineTokenGA clickUseSlotMachineTokenGA)
    {
        DataManager.Instance.GameModel.ClickedKeywords.Clear ();
    }

    private IEnumerator RemoveKeywordPerformer(RemoveSlotMachineKeywordGA removeSlotMachineKeywordGA)
    {
        if (removeSlotMachineKeywordGA.Cost > 0)
        {
            ActionSystem.Instance.AddReaction (new ApplyGoldDeltaGA (-removeSlotMachineKeywordGA.Cost));
        }

        EKeyword keyword = removeSlotMachineKeywordGA.Keyword;
        bool isRemoved = false;
        var model = DataManager.Instance.GameModel;
        SO_KeywordData data = DataManager.Instance.GetKeywordData (keyword);

        if (data != null)
        {
            switch (data.KeywordType)
            {
                case EKeywordType.Adverb:
                    isRemoved = model.AdverbKeywords.Remove (keyword);
                    if (!isRemoved) isRemoved = model.TempAdverbKeywords.Remove (keyword);
                    break;
                case EKeywordType.Verb:
                    isRemoved = model.VerbKeywords.Remove (keyword);
                    if (!isRemoved) isRemoved = model.TempVerbKeywords.Remove (keyword);
                    break;
                case EKeywordType.Curse:
                    isRemoved = model.CurseKeywords.Remove (keyword);
                    if (!isRemoved) isRemoved = model.TempCurseKeywords.Remove (keyword);
                    break;
            }
        }

        yield return null;
    }

    private IEnumerator LevelUpKeywordPerformer(LevelUpKeywordGA levelUpKeywordGA)
    {
        bool isValid = true;

        SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(levelUpKeywordGA.UpgradeKeyword);

        if(keywordData == null)
        {
            isValid = false;
        }
        else
        {
            if (keywordData.UpgradedId == 0)
            {
                isValid = false;
            }

            if (keywordData.KeywordType == EKeywordType.Adverb && DataManager.Instance.GameModel.AdverbKeywords.Contains(levelUpKeywordGA.UpgradeKeyword) == false)
            {
                isValid = false;
            }

            if (keywordData.KeywordType == EKeywordType.Verb && DataManager.Instance.GameModel.VerbKeywords.Contains(levelUpKeywordGA.UpgradeKeyword) == false)
            {
                isValid = false;
            }
        }

        if(isValid == false)
        {
            EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_SLOTMACHINESYSTEM_018"), EMessageType.Warning));
            yield break;
        }

        // 해당 키워드 지워주고
        RemoveSlotMachineKeywordGA removeSlotMachineKeywordGA = new RemoveSlotMachineKeywordGA(levelUpKeywordGA.UpgradeKeyword);
        ActionSystem.Instance.AddReaction(removeSlotMachineKeywordGA);

        // 업그레이드된 키워드를 추가한다.
        SO_KeywordData upgradeKeywordData = DataManager.Instance.GetKeywordData(keywordData.UpgradedId);
        AddSlotMachineKeywordGA addSlotMachineKeywordGA = new AddSlotMachineKeywordGA(upgradeKeywordData.Keyword);
        ActionSystem.Instance.AddReaction(addSlotMachineKeywordGA);
    }

    public void SpinAllSlotsInstant(float higherTierWeightMultiplier = 1f)
    {
        _slotMachineResult = _engine.PickOne(higherTierWeightMultiplier);
    }

    public bool CurrentResultHasAllUniqueKeywords()
    {
        if (_slotMachineResult?.reelResult == null)
        {
            return false;
        }

        HashSet<EKeyword> seen = new HashSet<EKeyword>();
        for (int y = 0; y < _slotMachineResult.reelResult.GetLength(0); y++)
        {
            for (int x = 0; x < _slotMachineResult.reelResult.GetLength(1); x++)
            {
                EKeyword keyword = _slotMachineResult.reelResult[y, x];
                if (keyword == EKeyword.None)
                {
                    continue;
                }

                if (!seen.Add(keyword))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public List<BattleAct> BuildPlayerBattleActsFromCurrentResult()
    {
        List<BattleAct> battleActs = new List<BattleAct>();
        if (_slotMachineResult?.bingoResult == null)
        {
            return battleActs;
        }

        foreach (BingoResult bingoResult in _slotMachineResult.bingoResult)
        {
            if (bingoResult == null || bingoResult.MatchType == EKeywordMatchType.NonMatch)
            {
                continue;
            }

            battleActs.Add(new BattleAct(bingoResult.Owner, bingoResult.Skill, true, bingoResult.Bingo));
        }

        return battleActs;
    }

    public bool CurrentResultHasKeywordRank(int rank)
    {
        if (_slotMachineResult?.reelResult == null || DataManager.Instance == null)
        {
            return false;
        }

        int height = _slotMachineResult.reelResult.GetLength(0);
        int width = _slotMachineResult.reelResult.GetLength(1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(_slotMachineResult.reelResult[y, x]);
                if (keywordData != null && keywordData.Rank == rank)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public List<BattleAct> RerollAllSlotsAndBuildPlayerBattleActs(int rerollCount, float higherTierWeightMultiplier = 1f)
    {
        if (_slotMachineResult == null || ArtifactRuntimeState.IsRerollDisabled)
        {
            return BuildPlayerBattleActsFromCurrentResult();
        }

        int count = Mathf.Max(1, rerollCount);
        for (int i = 0; i < count; i++)
        {
            CapturePreRerollSnapshot();
            _slotMachineResult = _engine.PickOne(higherTierWeightMultiplier);
            FinalizeReroll(GetAllSlotIndexes(), false);
        }

        return BuildPlayerBattleActsFromCurrentResult();
    }

    public List<BattleAct> RerollRandomSlotsAndBuildPlayerBattleActs(int slotCount)
    {
        if (_slotMachineResult == null || ArtifactRuntimeState.IsRerollDisabled)
        {
            return BuildPlayerBattleActsFromCurrentResult();
        }

        CapturePreRerollSnapshot();

        List<int> slotIndexes = GetAllSlotIndexes();
        slotIndexes.Shuffle();
        slotIndexes = slotIndexes.Take(Mathf.Clamp(slotCount, 1, slotIndexes.Count)).ToList();

        foreach (int slotIndex in slotIndexes)
        {
            int x = slotIndex % SO_SlotMachineConfig.HORIZONTAL;
            int y = slotIndex / SO_SlotMachineConfig.VERTICAL;
            _slotMachineResult.reelResult[y, x] = _engine.GetKeywordsByX((EKeywordTypePos)x).GetRandomElement();
        }

        FinalizeReroll(slotIndexes, false);
        return BuildPlayerBattleActsFromCurrentResult();
    }

    private void CapturePreRerollSnapshot()
    {
        if (_slotMachineResult?.reelResult == null)
        {
            _preRerollSnapshotKeywords = null;
            return;
        }

        int height = _slotMachineResult.reelResult.GetLength(0);
        int width = _slotMachineResult.reelResult.GetLength(1);
        _preRerollSnapshotKeywords = new EKeyword[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                _preRerollSnapshotKeywords[y, x] = _slotMachineResult.reelResult[y, x];
            }
        }
    }

    private void FinalizeReroll(List<int> rerolledIndexes, bool isClickReroll)
    {
        if (_slotMachineResult == null)
        {
            return;
        }

        if (isClickReroll)
        {
            TryReintroduceHighestRankKeywordFromSnapshot(rerolledIndexes);
        }

        _battleRerollCount++;
        CurrentTurnRerollCount++;
        _rerolledSlotIndexesThisTurn.UnionWith(rerolledIndexes);

        int nthRerollInterval = ArtifactRuntimeState.UpgradeAllSlotsOnNthRerollInterval;
        if (nthRerollInterval > 0 && _battleRerollCount % nthRerollInterval == 0)
        {
            OverwriteAllSlotsWithHighestOwnedKeywords();
        }

        _slotMachineResult.bingoResult = _engine.Judge();
        QueueRerollDamage();
    }

    private List<int> GetAllSlotIndexes()
    {
        return Enumerable.Range(0, SO_SlotMachineConfig.HORIZONTAL * SO_SlotMachineConfig.VERTICAL).ToList();
    }

    private void TryReintroduceHighestRankKeywordFromSnapshot(List<int> rerolledIndexes)
    {
        if (_preRerollSnapshotKeywords == null ||
            rerolledIndexes == null ||
            rerolledIndexes.Count == 0 ||
            !ArtifactRuntimeState.RollChance(ArtifactRuntimeState.ClickRerollReintroduceChancePercent))
        {
            return;
        }

        Dictionary<EKeywordTypePos, List<int>> indexesByColumn = rerolledIndexes
            .GroupBy(index => (EKeywordTypePos)(index % SO_SlotMachineConfig.HORIZONTAL))
            .ToDictionary(group => group.Key, group => group.ToList());

        List<(EKeyword keyword, EKeywordTypePos pos)> candidates = new List<(EKeyword, EKeywordTypePos)>();
        foreach (KeyValuePair<EKeywordTypePos, List<int>> pair in indexesByColumn)
        {
            foreach (int slotIndex in pair.Value)
            {
                int x = slotIndex % SO_SlotMachineConfig.HORIZONTAL;
                int y = slotIndex / SO_SlotMachineConfig.VERTICAL;
                EKeyword keyword = _preRerollSnapshotKeywords[y, x];
                if (keyword != EKeyword.None)
                {
                    candidates.Add((keyword, pair.Key));
                }
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        int highestRank = candidates
            .Select(candidate => DataManager.Instance.GetKeywordData(candidate.keyword)?.Rank ?? 0)
            .Max();

        List<(EKeyword keyword, EKeywordTypePos pos)> highestRankCandidates = candidates
            .Where(candidate => (DataManager.Instance.GetKeywordData(candidate.keyword)?.Rank ?? 0) == highestRank)
            .ToList();

        if (highestRankCandidates.Count == 0)
        {
            return;
        }

        (EKeyword keyword, EKeywordTypePos pos) selected = highestRankCandidates.GetRandomElement();
        if (!indexesByColumn.TryGetValue(selected.pos, out List<int> matchingIndexes) || matchingIndexes.Count == 0)
        {
            return;
        }

        int targetSlotIndex = matchingIndexes.GetRandomElement();
        int targetX = targetSlotIndex % SO_SlotMachineConfig.HORIZONTAL;
        int targetY = targetSlotIndex / SO_SlotMachineConfig.VERTICAL;
        _slotMachineResult.reelResult[targetY, targetX] = selected.keyword;
    }

    private void OverwriteAllSlotsWithHighestOwnedKeywords()
    {
        if (_slotMachineResult?.reelResult == null)
        {
            return;
        }

        Dictionary<EKeywordTypePos, List<EKeyword>> highestKeywordsByType = new Dictionary<EKeywordTypePos, List<EKeyword>>
        {
            { EKeywordTypePos.Subject, GetHighestRankOwnedKeywords(DataManager.Instance.GameModel.SubjectKeywords, DataManager.Instance.GameModel.TempSubjectKeywords) },
            { EKeywordTypePos.Adverb, GetHighestRankOwnedKeywords(DataManager.Instance.GameModel.AdverbKeywords, DataManager.Instance.GameModel.TempAdverbKeywords) },
            { EKeywordTypePos.Verb, GetHighestRankOwnedKeywords(DataManager.Instance.GameModel.VerbKeywords, DataManager.Instance.GameModel.TempVerbKeywords) },
        };

        for (int y = 0; y < _slotMachineResult.reelResult.GetLength(0); y++)
        {
            for (int x = 0; x < _slotMachineResult.reelResult.GetLength(1); x++)
            {
                EKeywordTypePos typePos = (EKeywordTypePos)x;
                if (!highestKeywordsByType.TryGetValue(typePos, out List<EKeyword> keywords) || keywords.Count == 0)
                {
                    continue;
                }

                _slotMachineResult.reelResult[y, x] = keywords.GetRandomElement();
            }
        }
    }

    private List<EKeyword> GetHighestRankOwnedKeywords(IEnumerable<EKeyword> baseKeywords, IEnumerable<EKeyword> tempKeywords)
    {
        List<EKeyword> ownedKeywords = new List<EKeyword>();
        if (baseKeywords != null)
        {
            ownedKeywords.AddRange(baseKeywords.Where(keyword => keyword != EKeyword.None));
        }

        if (tempKeywords != null)
        {
            ownedKeywords.AddRange(tempKeywords.Where(keyword => keyword != EKeyword.None));
        }

        if (ownedKeywords.Count == 0)
        {
            return new List<EKeyword>();
        }

        int highestRank = ownedKeywords
            .Select(keyword => DataManager.Instance.GetKeywordData(keyword)?.Rank ?? 0)
            .Max();

        return ownedKeywords
            .Where(keyword => (DataManager.Instance.GetKeywordData(keyword)?.Rank ?? 0) == highestRank)
            .Distinct()
            .ToList();
    }

    private void TryTriggerGrowthPotionLevelUp()
    {
        if (!ArtifactRuntimeState.RollChance(ArtifactRuntimeState.GrowthPotionChancePercent) ||
            ArtifactRuntimeState.GrowthPotionLevelDiff <= 0 ||
            CharacterSystem.Instance == null)
        {
            return;
        }

        List<PlayerView> candidates = CharacterSystem.Instance.Players
            .Where(player => player != null && player.Player.IsMaxLevel == false)
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        PlayerView targetPlayer = candidates.GetRandomElement();
        ActionSystem.Instance.AddReaction(new LevelUpPlayerGA(ArtifactRuntimeState.GrowthPotionLevelDiff, targetPlayer, 0));
    }

    private void QueueRerollDamage()
    {
        if (ArtifactRuntimeState.DamageOnRerollPartyAttackRatio <= 0f ||
            CharacterSystem.Instance == null ||
            CharacterSystem.Instance.Enemies == null)
        {
            return;
        }

        List<CharacterView> livingEnemies = CharacterSystem.Instance.Enemies
            .Where(enemy => enemy != null && enemy.Character.IsDead == false)
            .Cast<CharacterView>()
            .ToList();

        if (livingEnemies.Count == 0)
        {
            return;
        }

        int baseDamage = Mathf.RoundToInt(GetAveragePartyAttackPower() * ArtifactRuntimeState.DamageOnRerollPartyAttackRatio);
        if (baseDamage <= 0)
        {
            return;
        }

        List<CharacterView> targets = livingEnemies
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(Mathf.Clamp(ArtifactRuntimeState.DamageOnRerollTargetCount, 1, livingEnemies.Count))
            .ToList();

        if (targets.Count == 0)
        {
            return;
        }

        DealDamageGA damageGA = new DealDamageGA(
            ArtifactExecutionContext.GetDefaultCaster(),
            targets,
            new DamageFormula(EDamageFormulaType.Flat, baseDamage));
        damageGA.MarkArtifactGenerated();
        ActionSystem.Instance.AddReaction(damageGA);
    }

    private int GetAveragePartyAttackPower()
    {
        if (CharacterSystem.Instance == null || CharacterSystem.Instance.Players.Count == 0)
        {
            return 0;
        }

        int totalAttack = 0;
        int aliveCount = 0;
        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView == null || playerView.Character.IsDead)
            {
                continue;
            }

            totalAttack += Mathf.RoundToInt(playerView.Player.GetStat(EStatType.AttackPower).Value);
            aliveCount++;
        }

        if (aliveCount == 0)
        {
            return 0;
        }

        return Mathf.RoundToInt(totalAttack / (float)aliveCount);
    }

    private bool IsRerollEffect(Effect effect)
    {
        return effect is RerollSlotMachineKeywordEffect ||
               effect is RerollSlotMachineKeywordAddTokenEffect ||
               effect is RerollSlotMachineKeywordAddTokenInBattlePhaseEffect ||
               effect is RerollSlotMachineLineEffect;
    }

    public bool WasKeywordRerolledThisTurn(Keyword keyword)
    {
        return keyword != null && _rerolledSlotIndexesThisTurn.Contains(keyword.SlotIndex);
    }

    public bool WasAnyCurrentSkillKeywordRerolledThisTurn(Skill skill)
    {
        if (skill == null)
        {
            return false;
        }

        return WasKeywordRerolledThisTurn(skill.AdverbKeyword) ||
               WasKeywordRerolledThisTurn(skill.VerbKeyword);
    }

    private void ShowRerollDisabledMessage()
    {
        EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_SLOTMACHINESYSTEM_019"), EMessageType.Warning));
    }
}
