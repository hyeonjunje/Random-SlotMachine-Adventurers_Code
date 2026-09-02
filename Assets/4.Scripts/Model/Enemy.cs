public class Enemy : Character
{
    public SO_EnemyData EnemyData { get; private set; }
    public EnemyAI EnemyAI { get; private set; }

    public Enemy(SO_EnemyData enemyData) : base(enemyData, EBattleSideType.EnemySide)
    {
        EnemyData = enemyData;
    }

    public void SetEnemyAI(EnemyView enemyView)
    {
        EnemyAI = new EnemyAI(enemyView, EnemyData.EnemyAI);
    }

    public override void Release()
    {
        base.Release();

        if(EnemyData.AbilityData != null)
        {
            Ability.Release();
        }

        EnemyAI.Release();
    }

    public override void StartTurn()
    {
        base.StartTurn();

        EnemyAI.NextEnemyAct();
    }

    public override string GetName()
    {
        return LocalizationManager.Instance.Get(EnemyData.Name);
    }
}