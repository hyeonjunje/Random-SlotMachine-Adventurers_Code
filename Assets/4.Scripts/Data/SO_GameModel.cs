using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Runtime game model data that persists current run state.
/// </summary>
[CreateAssetMenu(fileName = "SO_GameModel", menuName = "Scriptable Objects/SO_GameModel")]
public class SO_GameModel : ScriptableObject
{
    [field: SerializeField, Header("Keywords")] public List<EKeyword> SubjectKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> TempSubjectKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> AdverbKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> TempAdverbKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> VerbKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> TempVerbKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> CurseKeywords = new List<EKeyword>();
    [field: SerializeField] public List<EKeyword> TempCurseKeywords = new List<EKeyword>();

    [Header("Score")]
    public float ElapsedTime = 0f;
    public int EnteredIslandCount = 0;
    public int GainedGold = 0;
    public int GainedArtifact = 0;
    public int GainedKeyword = 0;

    [Header("Info")]
    public bool IsAllowDiagonal = false;
    public int Stage = 0;
    public int Floor = 1;
    public int KeywordUpgradeOptionCount = 3;
    public int WordRemovalBuyCount = 0;

    [field: SerializeField, Header("Slot Machine Success Probabilities")] public float SuccessProbability { get; set; } = 0.5f;
    [field: SerializeField] public float GreatSuccessProbability { get; set; } = 0.3f;
    [field: SerializeField] public float UltraSuccessProbability { get; set; } = 0.2f;
    [field: SerializeField] public float FailureProbability { get; set; } = 0f;

    [field: SerializeField, Header("Values")] public float WeakeningValue = 0f;
    [field: SerializeField] public float MarkingValue = 0f;
    [field: SerializeField] public float EletricValue = 0f;
    [field: SerializeField] public float CounterAttackValue = 0f;
    [field: SerializeField] public float PunishmentAttackValue = 0f;
    [field: SerializeField] public float GuardianValue = 0f;
    [field: SerializeField] public float PreservationValue = 0f;

    [field: SerializeField] public float DealDamageExtraValue = 0f;
    [field: SerializeField] public float AddShieldExtraValue = 0f;
    [field: SerializeField] public float RestHealingValue = 0.3f;
    [field: SerializeField] public float ApplyHealingExtraValue = 0f;

    [field: SerializeField] public float EarnedMoneyAmount = 1f;

    [field: SerializeField, Header("Level Up Rank Weights")] public List<float> LevelUpRankWeights { get; set; } = new List<float> { 60f, 30f, 10f };

    [field: SerializeField, Header("Recent Click")] public EBingo RecentlyClickedBingo { get; set; } = EBingo.None;
    public List<Keyword> ClickedKeywords { get; set; } = new List<Keyword>();

    [field: SerializeField] public SerializableMapData MapData { get; set; }

    private CancellationTokenSource _countingElapsedTimeCts;

    /// <summary>
    /// Saves runtime map data into the serialized model.
    /// </summary>
    public void SaveMapData(MapData data)
    {
        if (data == null)
        {
            Debug.LogError("SaveMapData failed because MapData is null.");
            return;
        }

        MapData = new SerializableMapData(data);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log("MapData saved into SO_GameModel.");
    }

    /// <summary>
    /// Loads runtime map data from the serialized model.
    /// </summary>
    public MapData LoadMapData()
    {
        MapData loadedData = MapData.ToMapData();
        Debug.Log("MapData loaded from SO_GameModel.");
        return loadedData;
    }

    /// <summary>
    /// Returns whether serialized map data exists.
    /// </summary>
    public bool HasMapData()
    {
        return MapData != null && MapData.nodes != null && MapData.nodes.Count > 0;
    }

    // 경과시간 카운팅
    public void CountElapsedTime()
    {
        if(_countingElapsedTimeCts != null)
        {
            _countingElapsedTimeCts?.Cancel();
            _countingElapsedTimeCts?.Dispose();
        }

        _countingElapsedTimeCts = new CancellationTokenSource();

        AsyncCountElapsedTime(_countingElapsedTimeCts.Token).Forget();
    }

    private async UniTaskVoid AsyncCountElapsedTime(CancellationToken token)
    {
        while(true)
        {
            DataManager.Instance.GameModel.ElapsedTime += Time.unscaledDeltaTime;
            await UniTask.Yield(cancellationToken: token);
        }
    }
}
