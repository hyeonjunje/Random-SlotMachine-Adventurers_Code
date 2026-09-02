public class SpawnPlayerGA : GameAction
{
    public Player Player { get; private set; }

    public SpawnPlayerGA(Player player)
    {
        Player = player;
    }
}
