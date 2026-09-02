using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 해당 스크립트의 필드명은 수정하면 안됩니다.
/// 인스펙터에서 깨지기는 물론 에디터 툴이 해당 필드의 이름을 보고 동작하기 때문입니다.
/// </summary>
[CreateAssetMenu(fileName = "SO_DB", menuName = "Scriptable Objects/SO_DB")]
public class SO_DB : ScriptableObject
{
    [field: SerializeField] public SO_PlayerData[] AllPlayerData { get; private set; }
    [field: SerializeField] public SO_EnemyData[] AllEnemyData { get; private set; }
    [field: SerializeField] public SO_EventData[] AllEventData { get; private set; }
    [field: SerializeField] public SO_EventData StartEvent { get; private set; } // 시작 이벤트 (슬더스 태초)
    [field: SerializeField] public SO_ArtifactData[] AllArtifacts { get; private set; }
    [field: SerializeField] public List<SO_SkillData> AllPlayerSkills { get; private set; }

    [field: SerializeField] public List<SO_StatusData> AllStatuses { get; private set; }
    [field: SerializeField] public List<SO_StageData> AllStageData { get; private set; }

    [field: SerializeField] public SO_KeywordData[] SubjectKeywordData { get; private set; } // 주어 키워드
    [field: SerializeField] public SO_KeywordData[] AdverbKeywordData { get; private set; }  // 부사 키워드
    [field: SerializeField] public SO_KeywordData[] VerbKeywordData { get; private set; }    // 동사 키워드
    [field: SerializeField] public SO_KeywordData[] CurseKeywordData { get; private set; }   // 저주 키워드
}
