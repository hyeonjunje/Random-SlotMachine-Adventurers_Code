using System.Collections;
public class SpawnEnemyGA : GameAction
{
    public Enemy Enemy { get; private set; }
    public int PosIndex { get; private set; }

    public SpawnEnemyGA(Enemy enemy, int posIndex)
    {
        Enemy = enemy;
        PosIndex = posIndex;
    }
}
