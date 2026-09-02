using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 전투의 흐름과 데이터를 관리하는 스크립트
public partial class BattleSystem : SingletonScene<BattleSystem>
{
    private Queue<BattleAct> _actQueue = new Queue<BattleAct>();
    private EMapNodeType _currentBattleType;
    private int _baseSpeedActionCount = 5;
    private int _maxSpeedActionCount = 20;

    public List<EnemyView> CurrentTargets { get; set; } = new List<EnemyView>(); // 현재 타겟
    public List<CharacterView> RecentlyTargets { get; set; } = new List<CharacterView>(); // 가장 최근 DealDamageGA의 TargetSelector로 구한 타겟
    public CharacterView RecentlyCaster { get; set; } = null;  // 가장 최근 DealDamageGA의 Caster인 CharacterView
    public int RecentlyOriginDealDamage { get; set; } = 0; // 가장 최근 DealDamageGA로 계산된 원본 데미지
    public int RecentlyRealDealDamage { get; set; } = 0;   // 가장 최근 DealDamageGA로 계산된 실제 최종 데미지

    public int CurrentTurn { get; private set; } = 0;
    public int CurrentTurnPartyActionCount { get; private set; } = 0;
    public int CurrentTurnAttackCount { get; private set; } = 0;
    public BattleAct LastExecutedPlayerBattleAct { get; private set; } = null;
    public int CurrentBattleAttackCount { get; private set; } = 0;
    public int TotalSlotConfirmCount { get; private set; } = 0;
    public BattleAct CurrentExecutingBattleAct { get; private set; } = null;
    public EMapNodeType CurrentBattleType => _currentBattleType;

    private List<BattleAct> _currentConfirmedBattleActs = new List<BattleAct>();
    public IReadOnlyList<BattleAct> CurrentConfirmedBattleActs => _currentConfirmedBattleActs;
    public bool IsTutorialBattle { get; private set; }

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();


        ActionSystem.AttachPerformer<PrepareBattleGA>(PrepareBattlePerformer);
        // 전투 흐름 관련 GA
        ActionSystem.AttachPerformer<StartBattleGA>(StartBattlePerformer);
        ActionSystem.AttachPerformer<StartTurnGA>(StartTurnPerformer);
        ActionSystem.AttachPerformer<StartAutoBattleGA>(StartAutoBattlePerformer);
        ActionSystem.AttachPerformer<EndTurnGA>(EndTurnPerformer);

        ActionSystem.AttachPerformer<ClearBattleGA>(ClearBattlePerformer);
        ActionSystem.AttachPerformer<GrantPostBattleGoldGA>(GrantPostBattleGoldPerformer);
        ActionSystem.AttachPerformer<GameOverGA>(GameOverPerformer);


        // 자동 전투 관련 GA
        ActionSystem.AttachPerformer<ActAutoBattleGA>(ActAutoBattlePerformer);
        ActionSystem.AttachPerformer<RepeatLastBattleActGA>(RepeatLastBattleActPerformer);

        // Subscribe
        ActionSystem.SubscribeReaction<ActAutoBattleGA>(SubscribeActAutoBattleGA, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<ClearBattleGA>(SubscribeClearBattleGA, EReactionTiming.Post);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<PrepareBattleGA>();
        ActionSystem.DetachPerformer<StartBattleGA>();
        ActionSystem.DetachPerformer<StartTurnGA>();
        ActionSystem.DetachPerformer<StartAutoBattleGA>();
        ActionSystem.DetachPerformer<EndTurnGA>();

        ActionSystem.DetachPerformer<ClearBattleGA>();
        ActionSystem.DetachPerformer<GameOverGA>();

        ActionSystem.DetachPerformer<PerformEffectGA>();
        ActionSystem.DetachPerformer<ActAutoBattleGA>();
        ActionSystem.DetachPerformer<RepeatLastBattleActGA>();

        ActionSystem.DetachPerformer<GrantPostBattleGoldGA>();

        ActionSystem.UnSubscribeReaction<ActAutoBattleGA>(SubscribeActAutoBattleGA, EReactionTiming.Post);
    }

    public void InitCurrentTarget()
    {
        foreach (EnemyView enemyView in CharacterSystem.Instance.Enemies)
        {
            enemyView.SetTarget(false);
        }
        CurrentTargets.Clear();
        RecentlyTargets.Clear();
        RecentlyCaster = null;
        LastExecutedPlayerBattleAct = null;
        CurrentExecutingBattleAct = null;
        _currentConfirmedBattleActs.Clear();
    }

    public void BeginTutorialBattle()
    {
        PrepareTutorialBattleView();
        ActionSystem.Instance.Perform(new StartBattleGA());
    }

    public void PrepareTutorialBattleView()
    {
        IsTutorialBattle = true;
        CurrentTurn = 0;
        CurrentBattleAttackCount = 0;
        TotalSlotConfirmCount = 0;
        _currentBattleType = EMapNodeType.Monster;

        InitCurrentTarget();
        ChangeBattleState(EBattleState.StartBattle);

        UIManager.Instance.Open(EUIType.UI_Battle);
        EventBus.Publish(new StEnterBattleNodeEvent());
    }

    public void EndTutorialBattle()
    {
        IsTutorialBattle = false;
        _actQueue.Clear();
        CurrentTurn = 0;
        CurrentTurnPartyActionCount = 0;
        CurrentTurnAttackCount = 0;
        CurrentBattleAttackCount = 0;
        TotalSlotConfirmCount = 0;
        ChangeBattleState(EBattleState.NonBattle);
        InitCurrentTarget();
    }

    public void UpdateActQueue(List<BattleAct> battleActs)
    {
        _actQueue.Clear();
        foreach (BattleAct battleAct in battleActs)
        {
            _actQueue.Enqueue(battleAct);
        }
    }

    public void ReplaceRemainingPlayerActs(List<BattleAct> replacementPlayerActs)
    {
        if (replacementPlayerActs == null)
        {
            return;
        }

        List<BattleAct> currentQueue = _actQueue.ToList();
        Queue<BattleAct> rebuiltQueue = new Queue<BattleAct>();
        int replacementIndex = 0;

        foreach (BattleAct queuedAct in currentQueue)
        {
            if (queuedAct != null && queuedAct.IsPlayer)
            {
                if (replacementIndex < replacementPlayerActs.Count)
                {
                    rebuiltQueue.Enqueue(replacementPlayerActs[replacementIndex++]);
                }

                continue;
            }

            rebuiltQueue.Enqueue(queuedAct);
        }

        while (replacementIndex < replacementPlayerActs.Count)
        {
            rebuiltQueue.Enqueue(replacementPlayerActs[replacementIndex++]);
        }

        _actQueue = rebuiltQueue;

        int currentIndex = CurrentExecutingBattleAct == null
            ? -1
            : _currentConfirmedBattleActs.FindIndex(act => ReferenceEquals(act, CurrentExecutingBattleAct));

        if (currentIndex >= 0)
        {
            List<BattleAct> rebuiltConfirmed = new List<BattleAct>();
            rebuiltConfirmed.AddRange(_currentConfirmedBattleActs.Take(currentIndex + 1));

            replacementIndex = 0;
            foreach (BattleAct queuedAct in currentQueue)
            {
                if (queuedAct != null && queuedAct.IsPlayer)
                {
                    if (replacementIndex < replacementPlayerActs.Count)
                    {
                        rebuiltConfirmed.Add(replacementPlayerActs[replacementIndex++]);
                    }

                    continue;
                }

                rebuiltConfirmed.Add(queuedAct);
            }

            while (replacementIndex < replacementPlayerActs.Count)
            {
                rebuiltConfirmed.Add(replacementPlayerActs[replacementIndex++]);
            }

            _currentConfirmedBattleActs = rebuiltConfirmed;
        }
    }

    private IEnumerator PrepareBattlePerformer(PrepareBattleGA prepareBattleGA)
    {
        foreach(PlayerView player in CharacterSystem.Instance.Players)
        {
            player.PrepareBattle();
        }

        yield return null;

        _actQueue.Clear();

        ChangeBattleState(EBattleState.StartBattle);

        UIManager.Instance.Open(EUIType.UI_Battle);

        EventBus.Publish(new StEnterBattleNodeEvent()); // 캐릭터 위치 조정
        CurrentTurn = 0;
        CurrentBattleAttackCount = 0;
        TotalSlotConfirmCount = 0;

        InitCurrentTarget();

        _currentBattleType = prepareBattleGA.BattleType;

        MatchupEnemyBundle matchupEnemyBundle = prepareBattleGA.MatchupEnemyBundle;

        for (int i = 0; i < matchupEnemyBundle.MatchupEnemies.Length; ++i)
        {
            MatchupEnemy matchupEnemy = matchupEnemyBundle.MatchupEnemies[i];

            Enemy enemy = new Enemy(matchupEnemy.Enemy);
            SpawnEnemyGA spawnEnemyGA = new SpawnEnemyGA(enemy, matchupEnemy.EnemyPosIndex);
            ActionSystem.Instance.AddReaction(spawnEnemyGA);
        }

        // 준비 끝나면 배틀 시작
        StartBattleGA startTurnGA = new StartBattleGA();
        ActionSystem.Instance.AddReaction(startTurnGA);
    }

    private IEnumerator StartBattlePerformer(StartBattleGA startBattleGA)
    {
        yield return new WaitForSeconds(UIManager.Instance.GetFadeDuration());

        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        yield return StartCoroutine(uiBattle.PlayStartBattleNotify());

        StartTurnGA startTurnGA = new StartTurnGA();
        ActionSystem.Instance.AddReaction(startTurnGA);

        // 전투 시작 시 예약된 상태이상 일괄 적용
        ApplyDelayedStatusGA applyDelayedStatus = new ApplyDelayedStatusGA();
        ActionSystem.Instance.AddReaction(applyDelayedStatus);
    }

    private IEnumerator StartTurnPerformer(StartTurnGA startTurnGA)
    {
        StyleManager.Instance.AnimationTimeData.SetBattleTimeScale(0);
        ChangeBattleState(EBattleState.StartTurn);

        CurrentTurn++;
        CurrentTurnPartyActionCount = 0;
        CurrentTurnAttackCount = 0;
        ArtifactRuntimeState.ResetTurnScopedState();

        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        yield return StartCoroutine(uiBattle.PlayTurnNotify(CurrentTurn));

        uiBattle.SetActiveStartSlotMachineButton(true);

        foreach (CharacterView characterView in CharacterSystem.Instance.Players)
        {
            characterView.StartTurn();
        }

        foreach (CharacterView characterView in CharacterSystem.Instance.Enemies)
        {
            characterView.StartTurn();
        }
    }

    private IEnumerator StartAutoBattlePerformer(StartAutoBattleGA startAutoBattleGA)
    {
        yield return null;

        ChangeBattleState(EBattleState.InAutoBattle);

        if (ArtifactRuntimeState.ShouldSkipPlayerActionsThisTurn())
        {
            startAutoBattleGA.BattleActs.RemoveAll(battleAct => battleAct != null && battleAct.IsPlayer);
            ArtifactRuntimeState.ConsumeSkipPlayerActionsTurn();
        }

        foreach (BattleAct battleAct in startAutoBattleGA.BattleActs)
        {
            _actQueue.Enqueue(battleAct);
        }


        // 자동 전투 진행
        _currentConfirmedBattleActs = new List<BattleAct>(startAutoBattleGA.BattleActs);
        TotalSlotConfirmCount++;

        if (_actQueue.Count == 0)
        {
            ActAutoBattleGA actAutoBattleGA = new ActAutoBattleGA(null);
            ActionSystem.Instance.AddReaction(actAutoBattleGA);
        }
        else
        {
            float timeScale = Mathf.InverseLerp(_baseSpeedActionCount, _maxSpeedActionCount, _actQueue.Count);
            StyleManager.Instance.AnimationTimeData.SetBattleTimeScale(timeScale);

            ActAutoBattleGA actAutoBattleGA = new ActAutoBattleGA(_actQueue.Dequeue());
            ActionSystem.Instance.AddReaction(actAutoBattleGA);
        }
    }

    private IEnumerator EndTurnPerformer(EndTurnGA endTurnGA)
    {
        InitCurrentTarget();

        StyleManager.Instance.AnimationTimeData.SetBattleTimeScale(0);
        yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.TurnEndDelayTime);

        foreach (CharacterView characterView in CharacterSystem.Instance.Players)
        {
            characterView.EndTurn();
        }

        foreach (CharacterView characterView in CharacterSystem.Instance.Enemies)
        {
            characterView.EndTurn();
        }

        if (ArtifactRuntimeState.UniqueSlotEndTurnMarkStacks > 0 &&
            SlotMachineSystem.Instance != null &&
            SlotMachineSystem.Instance.CurrentResultHasAllUniqueKeywords())
        {
            SO_StatusData markingStatus = DataManager.Instance.GetStatus(EStatusType.Marking);
            if (markingStatus != null)
            {
                List<CharacterView> targets = CharacterSystem.Instance.Enemies
                    .Where(enemy => enemy != null && enemy.Character.IsDead == false)
                    .Cast<CharacterView>()
                    .ToList();

                if (targets.Count > 0)
                {
                    ActionSystem.Instance.AddReaction(new AddStatusGA(
                        markingStatus,
                        ArtifactRuntimeState.UniqueSlotEndTurnMarkStacks,
                        targets,
                        ArtifactExecutionContext.GetDefaultCaster()));
                }
            }
        }

        if (BattleState == EBattleState.InAutoBattle)
        {
            StartTurnGA startTurnGA = new StartTurnGA();
            ActionSystem.Instance.AddReaction(startTurnGA);
        }
        else
        {

        }
    }

    private IEnumerator ClearBattlePerformer(ClearBattleGA clearBattleGA)
    {
        yield return null;
        BattleState = EBattleState.ClearBattle;

        if (IsTutorialBattle)
        {
            UI_Battle tutorialBattleUI = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
            tutorialBattleUI?.InitTokenController();
            UIManager.Instance.Close(EUIType.UI_SlotMachine);

            UI_Tutorial tutorialUI = UIManager.Instance.Get<UI_Tutorial>(EUIType.UI_Tutorial);
            tutorialUI?.OnTutorialBattleCleared();
            yield break;
        }

        // 파티 초기화
        foreach (PlayerView player in CharacterSystem.Instance.Players)
        {
            player.EndBattle();
        }

        // 토큰 초기화
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.InitTokenController();

        // 2스테이지 보스 클리어 시 게임 클리어
        if(DataManager.Instance.GameModel.Stage == 2 && _currentBattleType == EMapNodeType.Boss)
        {
            UIManager.Instance.Open(EUIType.UI_Ending);
            UI_Ending uiEnding = UIManager.Instance.Get<UI_Ending>(EUIType.UI_Ending);
            uiEnding.SetEndindType(EEndingType.Victory);
            yield break;
        }

        if (_currentBattleType == EMapNodeType.Monster) // 일반 전투는 개별 레벨업
        {
            var players = CharacterSystem.Instance.Players;
            if (players != null && players.Count > 0)
            {
                var levelUpCandidates = players.Where (p => p.Player.IsMaxLevel == false).ToList ();
                if (levelUpCandidates.Count > 0)
                {
                    var sortedPlayers = levelUpCandidates.OrderBy (p => p.Player.Level)
                                                         .ThenBy (p => System.Guid.NewGuid ())
                                                         .ToList ();

                    List<float> weights = new List<float>();
                    var rankWeights = DataManager.Instance.GameModel.LevelUpRankWeights;

                    for (int i = 0; i < sortedPlayers.Count; i++)
                    {
                        float weight = (rankWeights != null && i < rankWeights.Count) ? rankWeights[i] : 10f;
                        weights.Add(weight);
                    }

                    PlayerView targetPlayer = sortedPlayers.PickWeighted(weights);

                    LevelUpPlayerGA playerLevelUpGA = new LevelUpPlayerGA(1, targetPlayer, 0);
                    ActionSystem.Instance.AddReaction(playerLevelUpGA);
                }
            }
        }
        else    // 엘리트, 보스 전투는 파티 레벨업
        {
            if (CharacterSystem.Instance.Players.Any (playerView => playerView.Player.IsMaxLevel == false))
            {
                LevelUpPartyGA partyLevelUpGA = new LevelUpPartyGA(1, 0);
                ActionSystem.Instance.AddReaction(partyLevelUpGA);
            }
        }

        // 보상 골드 계산
        int rewardGold = 0;
        switch (_currentBattleType)
        {
            case EMapNodeType.Monster: rewardGold = 5; break;
            case EMapNodeType.Elite: rewardGold = 10; break;
            case EMapNodeType.Boss: rewardGold = 30; break;
        }

        // 골드 지급 및 연출 대기
        if (rewardGold > 0)
        {
            GrantPostBattleGoldGA goldGA = new GrantPostBattleGoldGA(rewardGold);
            ActionSystem.Instance.AddReaction(goldGA);
        }

        BattleRewardData rewardData = new BattleRewardData();
        rewardData.Artifacts = new List<SO_ArtifactData>();

        // 일반 몬스터 전투
        if (_currentBattleType == EMapNodeType.Monster)
        {
            rewardData.RewardType = ERewardType.Normal;
            rewardData.Keywords = GetTier1Keywords(3);
        }
        else // Elite, Boss, Treasure 보상
        {
            rewardData.RewardType = ERewardType.Special;

            rewardData.Artifacts = ArtifactSystem.Instance.GetRandomRewardArtifacts(3);
        }

        UI_Reward rewardUI = UIManager.Instance.Get<UI_Reward>(EUIType.UI_Reward);

        AudioManager.Instance.PlaySFX(ESfxId.NormalBattle_Victory);
        rewardUI.SetReward(rewardData);
        UIManager.Instance.Open(EUIType.UI_Reward);
    }

    private IEnumerator GameOverPerformer(GameOverGA gameOverGA)
    {
        EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_BATTLESYSTEM_007"), EMessageType.Warning));
        yield return new WaitForSeconds(1f);

        SceneManagerEx.Instance.LoadScene(0);
    }

    private IEnumerator ActAutoBattlePerformer(ActAutoBattleGA actAutoBattleGA)
    {
        if (actAutoBattleGA.BattleAct == null)
        {
            yield break;
        }

        CharacterView caster = actAutoBattleGA.BattleAct.CharacterView;

        if (caster == null)
        {
            Debug.Log("해당 캐릭터가 없습니다.");
            yield break;
        }

        if (caster.Character.IsDead)
        {
            Debug.Log("죽었습니다.");
            yield break;
        }

        yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.ActIntervalTime);

        Skill currentSkill = actAutoBattleGA.BattleAct.Skill;
        Effect firstEffect = currentSkill.TotalEffect[0];
        CurrentExecutingBattleAct = actAutoBattleGA.BattleAct;
        if (actAutoBattleGA.BattleAct.IsPlayer)
        {
            LastExecutedPlayerBattleAct = actAutoBattleGA.BattleAct;
        }

        // 시전자가 우리 편이라면
        if (caster.Character.BattleSideType == EBattleSideType.OurSide)
        {
            // 해당 스킬이 행동카운트를 감소하는 스킬이라면
            if (currentSkill.IsDecreaseActCount)
            {
                // 적들의 행동카운트 제거
                foreach (EnemyView enemyView in CharacterSystem.Instance.Enemies)
                {
                    enemyView.Enemy.EnemyAI.ChangeActCount(-1);
                }
            }

            CurrentTurnPartyActionCount++;

            if (firstEffect is DealDamageEffect)
            {
                CurrentTurnAttackCount++;
                CurrentBattleAttackCount++;
            }
        }

        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        yield return uiBattle.CoUseToken();


        // 마비면 이번 행동 불가능
        if (caster.Character.IsStatus(EStatusType.Paralysis))
        {
            yield break;
        }

        // 가장 최근 공격 타겟과 시전자를 저장한다.
        foreach (Effect effect in currentSkill.TotalEffect)
        {
            if (effect is DealDamageEffect dealDamageEffect)
            {
                RecentlyTargets = effect.TargetSelector?.SelectTarget(caster);
                RecentlyCaster = caster;
            }
        }

        caster.SetAnimation(currentSkill.CharacterAnimationType);

        // 효과
        // 애니메이션 트리거까지의 시간
        yield return new WaitForSeconds(caster.AnimationController.GetTimeUntilEvent(currentSkill.CharacterAnimationType));

        // 사운드
        caster.PlayActSFX(currentSkill.CharacterAnimationType);

        // 행동
        foreach (Effect effect in currentSkill.TotalEffect)
        {
            List<CharacterView> targets = effect.TargetSelector?.SelectTarget(caster);

            PerformEffectGA performEffect = new PerformEffectGA(effect, targets, caster);
            ActionSystem.Instance.AddReaction(performEffect);
        }

        // 공격 후 대기 시간
        yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.AttackAnimationTime);
    }

    private IEnumerator RepeatLastBattleActPerformer(RepeatLastBattleActGA repeatLastBattleActGA)
    {
        if (LastExecutedPlayerBattleAct == null)
        {
            yield break;
        }

        int repeatCount = UnityEngine.Mathf.Max(0, repeatLastBattleActGA.RepeatCount);
        for (int i = 0; i < repeatCount; i++)
        {
            BattleAct copiedAct = new BattleAct(
                LastExecutedPlayerBattleAct.CharacterView,
                LastExecutedPlayerBattleAct.Skill,
                LastExecutedPlayerBattleAct.IsPlayer,
                LastExecutedPlayerBattleAct.Bingo);

            ActionSystem.Instance.AddReaction(new ActAutoBattleGA(copiedAct));
        }
    }

    private void SubscribeActAutoBattleGA(ActAutoBattleGA actAutoBattleGA)
    {
        DataManager.Instance.GameModel.DealDamageExtraValue = 0;
        DataManager.Instance.GameModel.AddShieldExtraValue = 0;
        DataManager.Instance.GameModel.ApplyHealingExtraValue = 0;
        CurrentExecutingBattleAct = null;

        // 전투 중이 아닐 때는 이후 로직을 진행하지 않는다.
        if (BattleState != EBattleState.InAutoBattle)
        {
            return;
        }

        if (_actQueue.Count == 0) // 행동이 없으면 턴 종료
        {
            EndTurnGA endTurnGA = new EndTurnGA();
            ActionSystem.Instance.AddReaction(endTurnGA);
        }
        else // 다음 행동이 있으면 실행
        {
            ActAutoBattleGA nextActAutoBattleGA = new ActAutoBattleGA(_actQueue.Dequeue());
            ActionSystem.Instance.AddReaction(nextActAutoBattleGA);
        }
    }
    
    private void SubscribeClearBattleGA(ClearBattleGA clearBattleGA)
    {
        if(_currentBattleType == EMapNodeType.Boss)
        {
            if(AppConfig.BootStrapperType != EBootstrapperType.Demo)
            {
                // 다음 스테이지 진행
                // UIManager.Instance.ShowSimplePopup(EPopupButtonType.One, LocalizationManager.Instance.Get("CS_BATTLESYSTEM_008"), rightButtonText: LocalizationManager.Instance.Get("CS_BATTLESYSTEM_006"), onClickRightButton: () => SceneManagerEx.Instance.LoadScene(0));
            }
        }
    }

    private IEnumerator GrantPostBattleGoldPerformer(GrantPostBattleGoldGA grantPostBattleGoldGA)
    {
        yield return null;

        int reward = grantPostBattleGoldGA.reward;
        if (reward > 0)
        {
            ActionSystem.Instance.AddReaction(new ApplyGoldDeltaGA(reward));

            UI_MainHud hud = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);
            if (hud != null)
            {
                Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
                hud.PlayGoldGainEffect(reward, screenCenter);
            }
        }
    }

    private List<SO_KeywordData> GetTier1Keywords(int count)
    {
        var tier1Pool = new List<SO_KeywordData>();

        tier1Pool.AddRange(DataManager.Instance.AllAdverbKeywords.Where(k => k.Rank == 1));
        tier1Pool.AddRange(DataManager.Instance.AllVerbKeywords.Where(k => k.Rank == 1));

        List<SO_KeywordData> result = new List<SO_KeywordData>();

        for (int i = 0; i < count; i++)
        {
            var picked = tier1Pool.GetRandomElement(result);

            if (picked != null)
            {
                result.Add(picked);
            }
            else
            {
                break;
            }
        }

        return result;
    }


    private void Update()
    {
        if (AppConfig.IsCheatEnabled && Input.GetKeyDown(KeyCode.K))
        {
            if (BattleState != EBattleState.ClearBattle)
            {
                StartCoroutine(ClearBattlePerformer(new ClearBattleGA()));
            }
        }
    }
}

