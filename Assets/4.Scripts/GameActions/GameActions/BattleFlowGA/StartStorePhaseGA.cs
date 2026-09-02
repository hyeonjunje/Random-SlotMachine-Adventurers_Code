public class StartStorePhaseGA : GameAction
{
    public readonly SO_CharacterData[] pool;
    public readonly bool isKeywordDuplicate;

    public StartStorePhaseGA() { }
    public StartStorePhaseGA(SO_CharacterData[] pool)
    {
        this.pool = pool;
    }
}