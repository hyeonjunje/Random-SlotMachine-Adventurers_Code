using SerializeReferenceEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_EventData", menuName = "Scriptable Objects/SO_EventData")]
public class SO_EventData : ScriptableObject
{
    [field:SerializeField] public int Id { get; private set; }
    [field:SerializeField] public string EventName { get; private set; }
    [field:SerializeField] public EEventRiskRewardType EventRiskRewardType { get; private set; }
    [field:SerializeField] public PageData[] PageDatas { get; private set; }
    [field: SerializeField] public EMiniGameType MiniGameType { get; private set; } = EMiniGameType.None;
}

// 이벤트의 페이지 데이터
[System.Serializable]
public class PageData
{
    [field: SerializeField] public int Id { get; private set; }
    [field: SerializeField] public bool IsStartPage { get; private set; } = false;
    [field: SerializeField, TextArea] public string EventExplain { get; private set; }
    [field: SerializeField] public Sprite EventSprite { get; private set; }
    [field: SerializeField] public ChoiceData[] Choices { get; private set; }
}

// 대화나 이벤트의 선택지 클래스
[System.Serializable]
public class ChoiceData
{
    [field:SerializeField] public string ChoiceExplain { get; private set; }
    [field: SerializeReference, SR] public Condition Condition { get; private set; }
    [field: SerializeReference, SR] public Effect[] Effects { get; private set; }
    [field:SerializeField, Header("성공확률 - 0이면 무조건 성공"), Range(0,1)] public float Probability { get; private set; }
    [field: SerializeReference, SR] public Effect[] FailedEffects { get; private set; }
}