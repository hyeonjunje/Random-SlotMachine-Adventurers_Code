public class MergeCharacterGA : GameAction
{
    public PlayerView TargetView; 
    public Player SourcePlayer;   

    public MergeCharacterGA(PlayerView targetView, Player sourcePlayer)
    {
        TargetView = targetView;
        SourcePlayer = sourcePlayer;
    }
}