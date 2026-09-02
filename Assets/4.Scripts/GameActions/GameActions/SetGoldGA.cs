public class SetGoldGA : GameAction
{
    public int Amount { get; private set; }

    public SetGoldGA(int amount)
    {
        Amount = amount;
    }
}
