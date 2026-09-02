public class RemoveSlotMachineKeywordGA : GameAction
{
    public EKeyword Keyword { get; private set; }
    public int Cost { get; private set; }

    public RemoveSlotMachineKeywordGA(EKeyword keyword, int cost = 0)
    {
        Keyword = keyword;
        Cost = cost;
    }
}