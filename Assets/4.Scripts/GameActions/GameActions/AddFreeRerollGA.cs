public class AddFreeRerollGA : GameAction
{
    public int Amount { get; private set; }
    public AddFreeRerollGA(int amount)
    {
        Amount = amount;
    }
}