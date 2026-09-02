using UnityEngine;

public static class NodeEntryCheckpointRunner
{
    public static void Start(NodeEntryCheckpoint checkpoint, SO_StageData stageData)
    {
        ExpeditionView expeditionView = Object.FindAnyObjectByType<ExpeditionView> (FindObjectsInactive.Include);
        expeditionView?.Hide ();
        UIManager.Instance.Close (EUIType.UI_Map);

        DataManager.Instance.GameModel.EnteredIslandCount++;

        switch (checkpoint.NodeType)
        {
            case EMapNodeType.Monster:
                AudioManager.Instance.PlayBGM(EBgmId.Battle);
                StartBattle (checkpoint, stageData);
                break;
            case EMapNodeType.Elite:
                AudioManager.Instance.PlayBGM(EBgmId.Elite);
                StartBattle (checkpoint, stageData);
                break;
            case EMapNodeType.Boss:
                AudioManager.Instance.PlayBGM(EBgmId.Boss);
                StartBattle (checkpoint, stageData);
                break;
            case EMapNodeType.Event:
                AudioManager.Instance.PlayBGM(EBgmId.Event);
                UIManager.Instance.Open (EUIType.UI_Event);
                UIManager.Instance.Get<UI_Event> (EUIType.UI_Event)
                    .Setup (DataManager.Instance.AllEvents[checkpoint.Event.EventIndex]);
                break;
            case EMapNodeType.Shop:
                AudioManager.Instance.PlayBGM(EBgmId.Shop);
                UIManager.Instance.Get<UI_Store> (EUIType.UI_Store).OpenFromSave (checkpoint.Shop);
                break;
            case EMapNodeType.Treasure:
                AudioManager.Instance.PlayBGM(EBgmId.Treasure);
                UIManager.Instance.Get<UI_Treasure> (EUIType.UI_Treasure).OpenFromSave (checkpoint.Treasure);
                break;
            case EMapNodeType.Rest:
                AudioManager.Instance.PlayBGM(EBgmId.Rest);
                EventBus.Publish (new StEnterRestNodeEvent ());
                break;
        }
    }

    private static void StartBattle(NodeEntryCheckpoint checkpoint, SO_StageData stageData)
    {
        MatchupEnemyBundle bundle = checkpoint.Battle.BattleType switch
        {
            EMapNodeType.Monster => stageData.MatchupDatas[checkpoint.GridY].MatchupEnemyBundles[checkpoint.Battle.MatchupIndex],
            EMapNodeType.Elite => stageData.EliteMatchupData.MatchupEnemyBundles[checkpoint.Battle.MatchupIndex],
            EMapNodeType.Boss => stageData.BossMatchupData.MatchupEnemyBundles[checkpoint.Battle.MatchupIndex],
            _ => null
        };

        if (bundle == null)
        {
            Debug.LogError ("NodeEntryCheckpointRunner.StartBattle failed: bundle is null.");
            return;
        }

        ActionSystem.Instance.Perform (new PrepareBattleGA (bundle, checkpoint.Battle.BattleType));
    }
}
