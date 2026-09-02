using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public const string TutorialClearedKey = "TutorialCleared";

    [Header("Tutorial Data")]
    [SerializeField] private SO_PlayerData _starterPlayer;
    [SerializeField] private SO_PlayerData[] _allyPlayers = new SO_PlayerData[2];
    [SerializeField] private SO_EnemyData _slimeEnemy;

    [Header("Dialogue")]
    [SerializeField] private TutorialDialogueUI _dialogueUI;

    private UI_Tutorial _ownerUI;
    private Coroutine _flowCoroutine;
    private ETutorialStep _currentStep = ETutorialStep.None;
    private ETutorialPhase _currentPhase = ETutorialPhase.None;
    private ETutorialWaitType _waitType = ETutorialWaitType.None;
    private bool _isRunning;
    private bool _firstPlayerAttackHandled;

    public ETutorialStep CurrentStep => _currentStep;
    public ETutorialPhase CurrentPhase => _currentPhase;
    public ETutorialWaitType WaitType => _waitType;
    public bool IsRunning => _isRunning;
    private bool IsFreeBattleUntilClear =>
        _isRunning &&
        _currentPhase == ETutorialPhase.FreeBattleUntilClear &&
        _waitType == ETutorialWaitType.BattleCleared;
    public bool CanStartSlotMachine =>
        _isRunning &&
        IsFreeBattleUntilClear == false &&
        _waitType == ETutorialWaitType.SlotSpinCompleted &&
        (_currentPhase == ETutorialPhase.Turn1Spin || _currentPhase == ETutorialPhase.Turn2PartySpin);
    public bool CanClickSlotConfirm =>
        _isRunning &&
        IsFreeBattleUntilClear == false &&
        ActionSystem.Instance != null &&
        ActionSystem.Instance.IsPerforming == false &&
        _waitType == ETutorialWaitType.SlotConfirmClicked;
    public bool ShouldHandleSlotConfirm =>
        _isRunning &&
        _waitType == ETutorialWaitType.SlotConfirmClicked &&
        _currentPhase == ETutorialPhase.Turn1TargetAndAttack;
    public bool ShouldBlockSlotConfirm =>
        _isRunning &&
        IsFreeBattleUntilClear == false &&
        CanClickSlotConfirm == false;
    public bool ShouldBlockSlotMachineStart =>
        _isRunning &&
        IsFreeBattleUntilClear == false &&
        CanStartSlotMachine == false;

    public void Initialize(UI_Tutorial ownerUI)
    {
        _ownerUI = ownerUI;
        if (_dialogueUI == null)
        {
            _dialogueUI = GetComponentInChildren<TutorialDialogueUI>(true);
        }

        if (_dialogueUI == null)
        {
            Debug.LogWarning($"{nameof(TutorialController)} needs a {nameof(TutorialDialogueUI)} reference.", this);
        }
    }

    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<ActAutoBattleGA>(OnActAutoBattlePost, EReactionTiming.Post);
    }

    private void OnDisable()
    {
        ActionSystem.UnSubscribeReaction<ActAutoBattleGA>(OnActAutoBattlePost, EReactionTiming.Post);
    }

    public void BeginTutorial()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _firstPlayerAttackHandled = false;
        StartFlow(CoBeginTutorial());
    }

    public void EndTutorial(bool markCleared)
    {
        if (markCleared)
        {
            PlayerPrefs.SetInt(TutorialClearedKey, 1);
            PlayerPrefs.Save();
        }

        StopFlow();

        _isRunning = false;
        _currentStep = ETutorialStep.None;
        _currentPhase = ETutorialPhase.None;
        _waitType = ETutorialWaitType.None;
        _dialogueUI?.Hide();

        CleanupTutorialRuntime();
    }

    public void OnClickDialogue()
    {
        if (!_isRunning)
        {
            return;
        }

        _dialogueUI?.Confirm();
    }

    public void OnTutorialBattleCleared()
    {
        if (!_isRunning)
        {
            return;
        }

        _waitType = ETutorialWaitType.None;
        StartFlow(CoCompleteTutorial());
    }

    public void OnBattleTokensCreated()
    {
        if (!_isRunning || _waitType != ETutorialWaitType.SlotSpinCompleted)
        {
            return;
        }

        _waitType = ETutorialWaitType.None;

        StartFlow(CoHandleBattleTokensCreated());
    }

    private IEnumerator CoHandleBattleTokensCreated()
    {
        yield return new WaitUntil(() => ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming == false);

        switch (_currentPhase)
        {
            case ETutorialPhase.Turn1Spin:
                yield return CoHandleTurn1SpinCompleted();
                break;
            case ETutorialPhase.Turn2PartySpin:
                EnterPhase(ETutorialPhase.FreeBattleUntilClear);
                WaitFor(ETutorialWaitType.BattleCleared);
                break;
        }
    }

    public bool TryHandleSlotConfirm(Action onReadyForTargetSelect = null)
    {
        if (!_isRunning)
        {
            onReadyForTargetSelect?.Invoke();
            return true;
        }

        if (_waitType != ETutorialWaitType.SlotConfirmClicked)
        {
            return false;
        }

        _waitType = ETutorialWaitType.None;
        StartFlow(CoHandleTurn1SlotConfirm(onReadyForTargetSelect));
        return true;
    }

    public void OnSlotConfirmButtonClicked()
    {
        TryHandleSlotConfirm();
    }

    public void SkipTutorial()
    {
        EndTutorial(true);
        OpenCharacterSelect();
    }

    private IEnumerator CoBeginTutorial()
    {
        EnterPhase(ETutorialPhase.Intro);

        ResolveMissingTutorialData();
        CharacterSystem.Instance.ClearTutorialCharacters();
        BattleSystem.Instance.PrepareTutorialBattleView();

        yield return SpawnPlayer(_starterPlayer);
        yield return SpawnEnemy();

        SetDialogueTargetToStarter();

        yield return ShowDialogueAndWait(ETutorialStep.Intro);
        yield return ShowDialogueAndWait(ETutorialStep.SpawnEnemy);

        yield return ShowDialogueAndWait(ETutorialStep.ExplainSingleLineSlot);

        EnterPhase(ETutorialPhase.Turn1Spin);
        WaitFor(ETutorialWaitType.SlotSpinCompleted);
        BattleSystem.Instance.BeginTutorialBattle();
    }

    private IEnumerator CoHandleTurn1SpinCompleted()
    {
        yield return ShowDialogueAndWait(ETutorialStep.ExplainCompletedSentence);

        EnterPhase(ETutorialPhase.Turn1TargetAndAttack);
        WaitFor(ETutorialWaitType.SlotConfirmClicked);
    }

    private IEnumerator CoHandleTurn1SlotConfirm(Action onReadyForTargetSelect)
    {
        yield return ShowDialogueAndWait(ETutorialStep.ExplainTargetSelect);

        WaitFor(ETutorialWaitType.PlayerActed);
        onReadyForTargetSelect?.Invoke();
    }

    private IEnumerator CoHandleFirstPlayerAttack()
    {
        yield return new WaitUntil(() => ActionSystem.Instance.IsPerforming == false);
        yield return new WaitUntil(() => BattleSystem.Instance.BattleState == EBattleState.StartTurn || BattleSystem.Instance.BattleState == EBattleState.ClearBattle);

        if (!_isRunning || BattleSystem.Instance.BattleState == EBattleState.ClearBattle)
        {
            yield break;
        }

        EnterPhase(ETutorialPhase.Turn2AlliesJoin);

        yield return ShowDialogueAndWait(ETutorialStep.AloneIsHard);

        foreach (SO_PlayerData allyPlayer in _allyPlayers)
        {
            yield return SpawnPlayer(allyPlayer);
        }

        SetDialogueTargetToStarter();
        yield return ShowDialogueAndWait(ETutorialStep.AlliesJoin);
        yield return ShowDialogueAndWait(ETutorialStep.ExplainPartySlot);

        EnterPhase(ETutorialPhase.Turn2PartySpin);
        WaitFor(ETutorialWaitType.SlotSpinCompleted);
    }

    private IEnumerator CoCompleteTutorial()
    {
        EnterPhase(ETutorialPhase.Complete);
        yield return ShowDialogueAndWait(ETutorialStep.Complete);

        _flowCoroutine = null;
        EndTutorial(true);
        OpenCharacterSelect();
    }

    private IEnumerator SpawnPlayer(SO_PlayerData playerData)
    {
        if (playerData == null)
        {
            yield break;
        }

        Player player = new Player(playerData);
        DataManager.Instance.GameModel.SubjectKeywords.Add(playerData.SubjectKeyword);

        bool done = false;
        ActionSystem.Instance.Perform(new SpawnPlayerGA(player), () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator SpawnEnemy()
    {
        if (_slimeEnemy == null)
        {
            yield break;
        }

        Enemy enemy = new Enemy(_slimeEnemy);
        bool done = false;
        ActionSystem.Instance.Perform(new SpawnEnemyGA(enemy, 1), () => done = true);
        yield return new WaitUntil(() => done);
    }

    private void OnActAutoBattlePost(ActAutoBattleGA actAutoBattleGA)
    {
        if (!_isRunning || _firstPlayerAttackHandled || _waitType != ETutorialWaitType.PlayerActed)
        {
            return;
        }

        if (_currentPhase != ETutorialPhase.Turn1TargetAndAttack ||
            actAutoBattleGA.BattleAct == null ||
            actAutoBattleGA.BattleAct.IsPlayer == false)
        {
            return;
        }

        _waitType = ETutorialWaitType.None;
        _firstPlayerAttackHandled = true;
        StartFlow(CoHandleFirstPlayerAttack());
    }

    private void EnterPhase(ETutorialPhase phase)
    {
        _currentPhase = phase;
        _waitType = ETutorialWaitType.None;
    }

    private void WaitFor(ETutorialWaitType waitType)
    {
        _waitType = waitType;
    }

    private IEnumerator ShowDialogueAndWait(ETutorialStep step)
    {
        _currentStep = step;
        WaitFor(ETutorialWaitType.DialogueConfirm);

        bool confirmed = false;
        ShowDialogue(step, () => confirmed = true);
        yield return new WaitUntil(() => confirmed);

        if (_waitType == ETutorialWaitType.DialogueConfirm)
        {
            _waitType = ETutorialWaitType.None;
        }
    }

    private void ShowDialogue(ETutorialStep step, Action onConfirmed)
    {
        string dialogueText = GetDialogueText(step);
        if (string.IsNullOrEmpty(dialogueText) || _dialogueUI == null)
        {
            onConfirmed?.Invoke();
            return;
        }

        _dialogueUI.Show(dialogueText, onConfirmed);
    }

    private void SetDialogueTargetToStarter()
    {
        PlayerView starterView = CharacterSystem.Instance.Players.FirstOrDefault();
        if (starterView != null)
        {
            _dialogueUI?.SetFollowTarget(starterView.transform);
        }
    }

    private string GetDialogueText(ETutorialStep step)
    {
        string key = step switch
        {
            ETutorialStep.Intro => "CS_TUTORIAL_001",
            ETutorialStep.SpawnEnemy => "CS_TUTORIAL_002",
            ETutorialStep.ExplainSingleLineSlot => "CS_TUTORIAL_003",
            ETutorialStep.ExplainCompletedSentence => "CS_TUTORIAL_004",
            ETutorialStep.ExplainTargetSelect => "CS_TUTORIAL_005",
            ETutorialStep.AloneIsHard => "CS_TUTORIAL_006",
            ETutorialStep.AlliesJoin => "CS_TUTORIAL_007",
            ETutorialStep.ExplainPartySlot => "CS_TUTORIAL_008",
            ETutorialStep.Complete => "CS_TUTORIAL_009",
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        return LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
    }

    private void OpenCharacterSelect()
    {
        UIManager.Instance.Open(EUIType.UI_SelectCharacter);
    }

    private void CleanupTutorialRuntime()
    {
        ActionSystem.Instance?.CancelAllActions();

        UI_Battle battleUI = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        battleUI?.InitTokenController();

        UIManager.Instance.Close(EUIType.UI_SlotMachine);
        UIManager.Instance.Close(EUIType.UI_Battle);
        UIManager.Instance.Close(EUIType.UI_MainHud);

        BattleView battleView = FindFirstObjectByType<BattleView>();
        battleView?.Hide();

        BattleSystem.Instance.EndTutorialBattle();
        CharacterSystem.Instance.ClearTutorialCharacters();

        _ownerUI?.Close();
    }

    private void ResolveMissingTutorialData()
    {
        if (DataManager.Instance == null)
        {
            return;
        }

        List<SO_PlayerData> players = DataManager.Instance.AllPlayers
            .Where(player => player != null)
            .OrderBy(player => player.Id)
            .ToList();

        if (_starterPlayer == null && players.Count > 0)
        {
            _starterPlayer = players[0];
        }

        if (_allyPlayers == null || _allyPlayers.Length < 2)
        {
            _allyPlayers = new SO_PlayerData[2];
        }

        for (int i = 0; i < _allyPlayers.Length; i++)
        {
            if (_allyPlayers[i] == null && players.Count > i + 1)
            {
                _allyPlayers[i] = players[i + 1];
            }
        }

        if (_slimeEnemy == null)
        {
            _slimeEnemy = DataManager.Instance.AllEnemies
                .Where(enemy => enemy != null)
                .OrderByDescending(enemy => enemy.name.Contains("슬라임") || enemy.name.Contains("Slime"))
                .ThenBy(enemy => enemy.Id)
                .FirstOrDefault();
        }
    }

    private void StartFlow(IEnumerator routine)
    {
        StopFlow();
        _flowCoroutine = StartCoroutine(routine);
    }

    private void StopFlow()
    {
        if (_flowCoroutine == null)
        {
            return;
        }

        StopCoroutine(_flowCoroutine);
        _flowCoroutine = null;
    }

}
