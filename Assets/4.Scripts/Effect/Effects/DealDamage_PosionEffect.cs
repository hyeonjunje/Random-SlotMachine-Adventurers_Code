using System.Collections.Generic;

public class DealDamage_PosionEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new DealDamage_PoisionGA(targets);
    }
}
