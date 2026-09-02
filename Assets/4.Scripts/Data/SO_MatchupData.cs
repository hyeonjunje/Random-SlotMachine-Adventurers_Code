using UnityEngine;

// 해당 매치업의 적 데이터 (별도 에셋용, StartBattleEffect 등에서 사용)
[CreateAssetMenu(fileName = "SO_MatchupData", menuName = "Scriptable Objects/SO_MatchupData")]
public class SO_MatchupData : ScriptableObject
{
    [field: SerializeField] public MatchupEnemyBundle MatchupEnemyBundle { get; private set; }
}

// 한 층에 직접 임베드되는 매치업 데이터
[System.Serializable]
public class FloorMatchupData
{
    [field: SerializeField] public MatchupEnemyBundle[] MatchupEnemyBundles { get; private set; }
}

// 한 전투에 나올 수 있는 적들의 데이터들
[System.Serializable]
public class MatchupEnemyBundle
{
    [field: SerializeField] public Sprite MatchupSprite { get; private set; }
    [field: SerializeField] public MatchupEnemy[] MatchupEnemies { get; private set; }
}

// 적과 적의 위치 인덱스
[System.Serializable]
public class MatchupEnemy
{
    [field: SerializeField] public SO_EnemyData Enemy { get; private set; }
    [field: SerializeField] public int EnemyPosIndex { get; private set; }
}
