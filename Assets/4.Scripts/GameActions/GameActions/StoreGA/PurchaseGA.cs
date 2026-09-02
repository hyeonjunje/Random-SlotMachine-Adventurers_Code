public class PurchaseCharacterGA : GameAction
{
    public Player Player;
    public int TargetIndex;
    public int Cost;

    public PurchaseCharacterGA(Player player, int index, int cost)
    {
        Player = player;
        TargetIndex = index;
        Cost = cost;
    }
}