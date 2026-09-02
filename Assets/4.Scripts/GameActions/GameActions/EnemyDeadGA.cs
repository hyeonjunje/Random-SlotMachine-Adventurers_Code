public class EnemyDeadGA : GameAction
{
    public CharacterView Killer { get; private set; }
    public EnemyView Killed { get; private set; }

    public EnemyDeadGA(CharacterView killer, EnemyView killed)
    {
        Killer = killer;
        Killed = killed;
    }
}