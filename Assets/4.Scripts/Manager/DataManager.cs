using System.Collections.Generic;
using UnityEngine;

public class DataManager : SingletonScene<DataManager>
{
    private const float DEFAULT_SUCCESS_PROBABILITY = 0.7f;
    private const float DEFAULT_GREAT_SUCCESS_PROBABILITY = 0.2f;
    private const float DEFAULT_ULTRA_SUCCESS_PROBABILITY = 0.1f;
    private const float DEFAULT_FAILURE_PROBABILITY = 0f;

    [SerializeField] private SO_DB _db;
    [field:SerializeField] public SO_GameModel GameModel { get; private set; }

    public IReadOnlyList<SO_PlayerData> AllPlayers => _db.AllPlayerData;
    public IReadOnlyList<SO_EnemyData> AllEnemies => _db.AllEnemyData;
    public IReadOnlyList<SO_EventData> AllEvents => _db.AllEventData;
    public IReadOnlyList<SO_ArtifactData> AllArtifacts => _db.AllArtifacts ?? System.Array.Empty<SO_ArtifactData> ();
    public IReadOnlyList<SO_StageData> AllStageData => _db.AllStageData;

    public IReadOnlyList<SO_KeywordData> AllSubjectKeywords => _db.SubjectKeywordData;
    public IReadOnlyList<SO_KeywordData> AllAdverbKeywords => _db.AdverbKeywordData;
    public IReadOnlyList<SO_KeywordData> AllVerbKeywords => _db.VerbKeywordData;
    public IReadOnlyList<SO_KeywordData> AllCurseKeywords => _db.CurseKeywordData;
    public SO_EventData StartEvent => _db.StartEvent;

    private Dictionary<EKeyword, SO_KeywordData> _dictKeywords = new();
    private Dictionary<int, SO_KeywordData> _dictKeywordsByIndex = new();
    private Dictionary<EStatusType, SO_StatusData> _statusData = new();

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();

        InitData();
        SetData();
    }

    private void InitData()
    {
        GameModel.SubjectKeywords.Clear();
        GameModel.AdverbKeywords.Clear();
        GameModel.VerbKeywords.Clear();
        GameModel.CurseKeywords.Clear();
        GameModel.TempCurseKeywords.Clear();
        _dictKeywords.Clear();
        _dictKeywordsByIndex.Clear();
        _statusData.Clear();

        GameModel.ElapsedTime = 0f;
        GameModel.EnteredIslandCount = 0;
        GameModel.GainedGold = 0;
        GameModel.GainedArtifact = 0;
        GameModel.GainedKeyword = 0;

        // ?뺣낫
        GameModel.IsAllowDiagonal = false;
        GameModel.Floor = 1;
        GameModel.KeywordUpgradeOptionCount = 3;

        GameModel.SuccessProbability = DEFAULT_SUCCESS_PROBABILITY;
        GameModel.GreatSuccessProbability = DEFAULT_GREAT_SUCCESS_PROBABILITY;
        GameModel.UltraSuccessProbability = DEFAULT_ULTRA_SUCCESS_PROBABILITY;
        GameModel.FailureProbability = DEFAULT_FAILURE_PROBABILITY;

        // ?섏튂
        GameModel.WeakeningValue = GameDefine.WEAKENING_VALUE;
        GameModel.MarkingValue = GameDefine.MARKING_VALUE;
        GameModel.EletricValue = GameDefine.ELETRIC_VALUE;
        GameModel.CounterAttackValue = GameDefine.COUNTERATTACK_VALUE;
        GameModel.PunishmentAttackValue = GameDefine.PUNISHMENTATTACK_VALUE;
        GameModel.GuardianValue = GameDefine.GUARDIAN_VALUE;
        GameModel.PreservationValue = GameDefine.PRESERVATION_VALUE;

        GameModel.DealDamageExtraValue = 0;
        GameModel.AddShieldExtraValue = 0;
        GameModel.ApplyHealingExtraValue = 0;

        GameModel.EarnedMoneyAmount = 1f;
    }

    private void SetData()
    {
        GameModel.AdverbKeywords.Add(EKeyword.그냥);
        GameModel.AdverbKeywords.Add(EKeyword.그냥);
        GameModel.AdverbKeywords.Add(EKeyword.그냥);
        GameModel.VerbKeywords.Add(EKeyword.공격해라);
        GameModel.VerbKeywords.Add(EKeyword.공격해라);
        GameModel.VerbKeywords.Add(EKeyword.공격해라);

        List<SO_KeywordData> allKeywords = new List<SO_KeywordData>();
        allKeywords.AddRange(AllSubjectKeywords);
        allKeywords.AddRange(AllAdverbKeywords);
        allKeywords.AddRange(AllVerbKeywords);
        allKeywords.AddRange(AllCurseKeywords);
        foreach (SO_KeywordData keywordData in allKeywords)
        {
            _dictKeywords[keywordData.Keyword] = keywordData;
            _dictKeywordsByIndex[keywordData.Id] = keywordData;
        }

        foreach(SO_StatusData statusData in _db.AllStatuses)
        {
            _statusData[statusData.StatusType] = statusData;
        }
    }

    public SO_KeywordData GetKeywordData(EKeyword keyword)
    {
        if (_dictKeywords.ContainsKey(keyword))
        {
            return _dictKeywords[keyword];
        }
        return null;
    }

    public SO_KeywordData GetKeywordData(int id)
    {
        if (_dictKeywordsByIndex.ContainsKey(id))
        {
            return _dictKeywordsByIndex[id];
        }
        return null;
    }

    public SO_StatusData GetStatus(EStatusType statusType)
    {
        if(_statusData.ContainsKey(statusType))
        {
            return _statusData[statusType];
        }
        return null;
    }
}

