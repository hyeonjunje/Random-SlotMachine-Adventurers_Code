public readonly struct StDecideBossMatchupEvent
{
    public readonly MatchupEnemyBundle BossMatchupEnemyBundle;

    public StDecideBossMatchupEvent(MatchupEnemyBundle bossMatchupEnemyBundle)
    {
        BossMatchupEnemyBundle = bossMatchupEnemyBundle;
    }
}
