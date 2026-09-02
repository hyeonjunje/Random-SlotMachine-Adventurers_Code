// 배틀 시작 전 준비 단계로 해당 배틀의 적을 소환해주고 배경을 세팅하는 작업을 합니다.
// 준비가 다 끝난 뒤 StartBattleGA를 호출합니다.
// 해당 장면은 섬 전환 시 Fade 효과에서 이루어집니다.
public class PrepareBattleGA : GameAction
{
    public MatchupEnemyBundle MatchupEnemyBundle { get; private set; }
    public EMapNodeType BattleType { get; private set; }

    public PrepareBattleGA(MatchupEnemyBundle matchupEnemyBundle, EMapNodeType battleType)
    {
        MatchupEnemyBundle = matchupEnemyBundle;
        BattleType = battleType;
    }
}
