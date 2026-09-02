using System.Collections.Generic;

public class TargetInBattleSelector : TargetSelector
{
    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        if (BattleSystem.Instance.CurrentTargets.Count != 0)
        {
            return new List<CharacterView>() { BattleSystem.Instance.CurrentTargets[0] };
        }
        else
        {
            return new List<CharacterView>() { null };
        }
    }
}
