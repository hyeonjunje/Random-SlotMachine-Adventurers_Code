using System.IO;
using UnityEngine;

public static class RunSaveService
{
    private static string SavePath => Path.Combine (Application.persistentDataPath, "run_save.json");

    public static bool HasSave()
    {
        return File.Exists (SavePath);
    }

    public static void SaveMapOnly(MapData mapData, int bossMatchupIndex)
    {
        if (mapData == null)
        {
            Debug.LogWarning ("SaveMapOnly skipped: mapData is null.");
            return;
        }

        RunSaveData saveData = new RunSaveData
        {
            SavePointType = ERunSavePointType.OnMap,
            BossMatchupIndex = bossMatchupIndex,
            Snapshot = CaptureSnapshot (mapData),
            Checkpoint = null
        };

        Write (saveData);
    }

    public static void SaveNodeEntry(MapData mapData, NodeEntryCheckpoint checkpoint, int bossMatchupIndex)
    {
        if (mapData == null || checkpoint == null)
        {
            Debug.LogWarning ("SaveNodeEntry skipped: mapData or checkpoint is null.");
            return;
        }

        RunSaveData saveData = new RunSaveData
        {
            SavePointType = ERunSavePointType.InNodeEntry,
            BossMatchupIndex = bossMatchupIndex,
            Snapshot = CaptureSnapshot (mapData),
            Checkpoint = checkpoint
        };

        Write (saveData);
    }

    public static bool TryLoad(out RunSaveData saveData)
    {
        saveData = null;

        if (!HasSave ())
        {
            return false;
        }

        string json = File.ReadAllText (SavePath);
        if (string.IsNullOrWhiteSpace (json))
        {
            return false;
        }

        saveData = JsonUtility.FromJson<RunSaveData> (json);
        return saveData != null;
    }

    public static void DeleteSave()
    {
        if (File.Exists (SavePath))
        {
            File.Delete (SavePath);
        }
    }

    private static RunSnapshot CaptureSnapshot(MapData mapData)
    {
        return new RunSnapshot
        {
            GameModel = GameModelSnapshot.Capture (DataManager.Instance.GameModel),
            Party = CharacterSystem.Instance.CapturePartySnapshot (),
            Inventory = new InventorySnapshot
            {
                Gold = UIHudSystem.Instance.CurrentGold,
                Artifacts = ArtifactSystem.Instance.CaptureArtifactSnapshots ()
            },
            MapData = new SerializableMapData (mapData)
        };
    }

    private static void Write(RunSaveData saveData)
    {
        string json = JsonUtility.ToJson (saveData, true);
        Directory.CreateDirectory (Path.GetDirectoryName (SavePath));
        File.WriteAllText (SavePath, json);
        Debug.Log ($"Run save written: {SavePath}");
    }
}
