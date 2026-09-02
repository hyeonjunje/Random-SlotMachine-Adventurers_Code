using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SlotMachineKeywordConfig
{
    public EEventSlotMachineKeyword Keyword;
    
    [Tooltip("뽑힐 확률 가중치 (높을수록 잘 나옵니다)")]
    public float Weight;
    
    [Tooltip("표시할 슬롯 아이콘(스프라이트)")]
    public Sprite Icon;

    [Tooltip("보상 수치 (예: 체력 증가량, 상승 레벨, 획득 골드 등)")]
    public int RewardValue;
}

[CreateAssetMenu(fileName = "SO_MiniGameSlotMachineConfig", menuName = "Scriptable Objects/SO_MiniGameSlotMachineConfig")]
public class SO_MiniGameSlotMachineConfig : ScriptableObject
{
    [SerializeField, Tooltip("각 키워드별 설정값. (확률 가중치 및 보상 수치)")]
    private List<SlotMachineKeywordConfig> _keywordConfigs = new List<SlotMachineKeywordConfig>();

    public IReadOnlyList<SlotMachineKeywordConfig> KeywordConfigs => _keywordConfigs;

    public EEventSlotMachineKeyword GetRandomKeyword()
    {
        if (_keywordConfigs == null || _keywordConfigs.Count == 0)
        {
            Debug.LogWarning("Slot Machine Configs are empty!");
            return EEventSlotMachineKeyword.Money;
        }

        float totalWeight = _keywordConfigs.Sum(c => c.Weight);
        if (totalWeight <= 0)
        {
            return _keywordConfigs[0].Keyword;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var config in _keywordConfigs)
        {
            currentWeight += config.Weight;
            if (randomValue <= currentWeight)
            {
                return config.Keyword;
            }
        }

        return _keywordConfigs.Last().Keyword;
    }

    public SlotMachineKeywordConfig GetConfigByKeyword(EEventSlotMachineKeyword keyword)
    {
        return _keywordConfigs.FirstOrDefault(c => c.Keyword == keyword);
    }
}