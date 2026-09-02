using System.Collections;
using UnityEngine;

public class StageSystem : SingletonScene<StageSystem>
{
    private SO_StageData _currentStageData;
    private MatchupEnemyBundle _bossMatchupData;

    private System.IDisposable _onMapStateUpdatedEvent;

    private MapNode _currentMapNode;
    private int _bossMatchupIndex;
    public int CurrentBossMatchupIndex => _bossMatchupIndex;

    private Coroutine _timerCoroutine;

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();

        ActionSystem.AttachPerformer<StartStageGA>(StartStagePerformer);
        ActionSystem.AttachPerformer<ClearNodeGA>(ClearNodePerformer);
        ActionSystem.AttachPerformer<LeaveNodeGA>(LeaveNodePerformer);

        _onMapStateUpdatedEvent = EventBus.Subscribe<StMapStateUpdatedEvent>(OnMapStateUpdatedEvent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        ActionSystem.DetachPerformer<StartStageGA>();
        ActionSystem.DetachPerformer<ClearNodeGA>();
        ActionSystem.DetachPerformer<LeaveNodeGA>();

        _onMapStateUpdatedEvent?.Dispose();
    }

    private IEnumerator StartStagePerformer(StartStageGA startStageGA)
    {
        UIManager.Instance.Close(EUIType.UI_Event);

        if (startStageGA.StageIndex == 0)
        {
            DataManager.Instance.GameModel.CountElapsedTime();
        }

        DataManager.Instance.GameModel.Stage = startStageGA.StageIndex;
        _currentStageData = DataManager.Instance.AllStageData[startStageGA.StageIndex];

        _bossMatchupIndex = Random.Range (0, _currentStageData.BossMatchupData.MatchupEnemyBundles.Length);
        _bossMatchupData = _currentStageData.BossMatchupData.MatchupEnemyBundles[_bossMatchupIndex];

        EventBus.Publish(new StCreateMapEvent(_currentStageData.MapConfigData));
        EventBus.Publish(new StDecideBossMatchupEvent(_bossMatchupData));

        yield return null;
    }

    private IEnumerator ClearNodePerformer(ClearNodeGA clearNodeGA)
    {
        UIManager.Instance.Close(EUIType.UI_Battle);
        UIManager.Instance.Close(EUIType.UI_Rest);
        UIManager.Instance.Close(EUIType.UI_SkillCard);
        UIManager.Instance.Close(EUIType.UI_Store);
        UIManager.Instance.Close(EUIType.UI_CharacterStore);
        UIManager.Instance.Close(EUIType.UI_SlotMachine);
        UIManager.Instance.Close(EUIType.UI_Event);
        UIManager.Instance.Close(EUIType.UI_Reward);
        UIManager.Instance.Close (EUIType.UI_Treasure);
        UIManager.Instance.Close (EUIType.UI_Reward);
        UIManager.Instance.Close (EUIType.UI_MyKeywords);
        UIManager.Instance.Close (EUIType.UI_KeywordUpgrade);
        UIManager.Instance.HideKeywordCardPreview();

        EventBus.Publish(new StClearNodeEvent(_currentMapNode.NodeType));

        LeaveNodeGA leaveNodeGA = new LeaveNodeGA();
        ActionSystem.Instance.AddReaction(leaveNodeGA);

        DataManager.Instance.GameModel.Floor++;

        if (_currentMapNode.NodeType == EMapNodeType.Boss)
        {
            // 스테이지 클리어 및 화면 전환 애니메이션

            // Fade Out 효과
            yield return StartCoroutine(UIManager.Instance.TransitionFadeOut());

            // Fade In 효과
            Utils.InvokeMethod(() =>
            {
                StartCoroutine(UIManager.Instance.TransitionFadeIn());
            }, 1000);

            int stageIndex = DataManager.Instance.GameModel.Stage + 1;
            StartStageGA startStageGA = new StartStageGA(stageIndex);
            ActionSystem.Instance.AddReaction(startStageGA);
        }

        yield return null;
    }

    private IEnumerator LeaveNodePerformer(LeaveNodeGA leaveNodeGA)
    {
        EventBus.Publish(new StLeaveNodeEvent(_currentMapNode));
        if (MapSystem.Instance.CurrentMapData != null && _currentMapNode != null)
        {
            MapSystem.Instance.CurrentMapData.SetCurrentNode (_currentMapNode);
            RunSaveService.SaveMapOnly (MapSystem.Instance.CurrentMapData, _bossMatchupIndex);
            Debug.Log ($"[LeaveNodePerformer] saved currentNode=({_currentMapNode.GridPosition.x},{_currentMapNode.GridPosition.y}) type={_currentMapNode.NodeType}");
        }

        yield return null;
    }

    private void OnMapStateUpdatedEvent(StMapStateUpdatedEvent mapStateUpdatedEvent)
    {
        _currentMapNode = mapStateUpdatedEvent.CurrentNode;

        ExpeditionView expeditionView = FindAnyObjectByType<ExpeditionView> (FindObjectsInactive.Include);
        expeditionView.Hide ();
        UIManager.Instance.Close (EUIType.UI_Map);

        bool shouldSkipNode = false;

        switch (_currentMapNode.NodeType)
        {
            case EMapNodeType.Monster:
            case EMapNodeType.Elite:
                shouldSkipNode = AppConfig.InGame.IsSkipBattle;
                break;
            case EMapNodeType.Boss:
                shouldSkipNode = AppConfig.InGame.IsSkipBoss;
                break;

            case EMapNodeType.Rest:
                shouldSkipNode = AppConfig.InGame.IsSkipRest;
                break;

            case EMapNodeType.Shop:
                shouldSkipNode = AppConfig.InGame.IsSkipShop;
                break;

            case EMapNodeType.Event:
                shouldSkipNode = AppConfig.InGame.IsSkipEvent;
                break;

            case EMapNodeType.Treasure:
                shouldSkipNode = AppConfig.InGame.IsSkipTreasure;
                break;
        }

        if (shouldSkipNode)
        {
            LeaveNodeGA leaveNodeGA = new LeaveNodeGA ();
            ActionSystem.Instance.Perform (leaveNodeGA);
            return;
        }

        NodeEntryCheckpoint checkpoint =
            NodeEntryCheckpointFactory.Create (_currentStageData, _currentMapNode, _bossMatchupIndex);

        RunSaveService.SaveNodeEntry (MapSystem.Instance.CurrentMapData, checkpoint, _bossMatchupIndex);
        NodeEntryCheckpointRunner.Start (checkpoint, _currentStageData);
    }

    public void RestoreStageForResume(int stageIndex, int bossMatchupIndex)
    {
        _currentStageData = DataManager.Instance.AllStageData[stageIndex];
        _bossMatchupIndex = bossMatchupIndex;
        _bossMatchupData = _currentStageData.BossMatchupData.MatchupEnemyBundles[_bossMatchupIndex];

        EventBus.Publish (new StDecideBossMatchupEvent (_bossMatchupData));
    }

    public void RestoreCurrentMapNode(MapNode currentNode)
    {
        _currentMapNode = currentNode;
    }
}
