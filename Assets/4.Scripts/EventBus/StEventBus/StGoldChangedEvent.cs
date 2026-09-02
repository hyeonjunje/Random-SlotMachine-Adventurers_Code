public readonly struct StGoldChangedEvent
{
    public readonly int CurrentGold;
    public readonly int Delta;       

    public StGoldChangedEvent(int currentGold, int delta)
    {
        CurrentGold = currentGold;
        Delta = delta;
    }
}