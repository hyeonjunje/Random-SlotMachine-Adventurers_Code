using UnityEngine;

public static class RunSaveLoader
{
    public static bool TryContinueLatest()
    {
        if (!RunSaveService.TryLoad (out RunSaveData saveData))
        {
            return false;
        }

        Apply (saveData);
        return true;
    }

    public static void Apply(RunSaveData saveData)
    {
        if (saveData?.Snapshot == null)
        {
            return;
        }

        RunSnapshot snapshot = saveData.Snapshot;

        snapshot.GameModel.ApplyTo (DataManager.Instance.GameModel);

        CharacterSystem.Instance.RestorePartySnapshot (snapshot.Party);
        UIHudSystem.Instance.RestoreGold (snapshot.Inventory?.Gold ?? 0);
        ArtifactSystem.Instance.RestoreArtifactSnapshots (snapshot.Inventory?.Artifacts);

        StageSystem.Instance.RestoreStageForResume (snapshot.GameModel.Stage, saveData.BossMatchupIndex);

        if (snapshot.MapData != null)
        {
            MapData loadedMapData = snapshot.MapData.ToMapData ();
            SO_StageData stageData = DataManager.Instance.AllStageData[snapshot.GameModel.Stage];

            loadedMapData.SetVisualData (
                stageData.MapConfigData.MapPrefab,
                stageData.MapConfigData.IslandColor,
                stageData.MapConfigData.IsNextLandColor);

            MapSystem.Instance.SetLoadedMapData (loadedMapData);
            StageSystem.Instance.RestoreCurrentMapNode (loadedMapData.CurrentNode);
        }

        UIManager.Instance.Open (EUIType.UI_MainHud);
        UI_MainHud mainHud = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        mainHud?.HideRightButton ();
        mainHud?.RefreshHpUIImmediately ();

        if (saveData.SavePointType == ERunSavePointType.InNodeEntry && saveData.Checkpoint != null)
        {
            SO_StageData stageData = DataManager.Instance.AllStageData[snapshot.GameModel.Stage];
            NodeEntryCheckpointRunner.Start (saveData.Checkpoint, stageData);
        }
        else
        {
            UIManager.Instance.Open (EUIType.UI_Map);
        }
    }
}
