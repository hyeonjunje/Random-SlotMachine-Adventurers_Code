public class PurchaseKeywordGA : GameAction
{
    public EKeyword NewKeyword;
    public int Cost;

    public PurchaseKeywordGA(EKeyword newKeyword, int cost)
    {
        NewKeyword = newKeyword;
        Cost = cost;
    }
}