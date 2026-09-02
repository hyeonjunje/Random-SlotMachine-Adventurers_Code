using System.Collections.Generic;

public class LevelUpPlayerGA : GameAction
{
    public int LevelDiff { get; private set; }
    public PlayerView TargetPlayer;
    public int Cost { get; private set; }


    public LevelUpPlayerGA(int levelDiff, PlayerView targetPlayer, int cost)
    {
        LevelDiff = levelDiff;
        TargetPlayer = targetPlayer;
        Cost = cost;
    }
}