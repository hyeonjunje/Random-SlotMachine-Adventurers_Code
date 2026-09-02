public class AddSlotMachineKeywordGA : GameAction
{
    public EKeyword Keyword { get; private set; }
    public int Cost { get; private set; }

    public AddSlotMachineKeywordGA(EKeyword keyword, int cost = 0)
    {
        Keyword = keyword;
        Cost = cost;
    }
}
