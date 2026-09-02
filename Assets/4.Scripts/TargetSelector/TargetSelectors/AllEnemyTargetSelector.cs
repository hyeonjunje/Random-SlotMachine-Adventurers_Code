using System.Collections.Generic;

public class AllEnemyTargetSelector : TargetSelector
{
    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        return new List<CharacterView>(CharacterSystem.Instance.Enemies);
    }
}
