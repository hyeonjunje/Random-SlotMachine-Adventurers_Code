public class LevelUpPartyGA : GameAction
{
    public int LevelDiff { get; private set; }
    public int Cost { get; private set; }

    public LevelUpPartyGA(int levelDiff = 1, int cost = 0)
    {
        LevelDiff = levelDiff;
        Cost = cost;
    }
}