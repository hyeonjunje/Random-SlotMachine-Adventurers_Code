public class AddSlotMachineTempKeywordGA : GameAction
{
    public EKeyword Keyword { get; private set; }

    public AddSlotMachineTempKeywordGA(EKeyword keyword)
    {
        Keyword = keyword;
    }
}
