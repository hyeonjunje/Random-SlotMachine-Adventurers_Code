using System.Collections.Generic;

public class PerformEffectGA : GameAction
{
    public Effect Effect { get; private set; }
    public CharacterView Caster { get; private set; }
    public List<CharacterView> Targets { get; private set; }

    public PerformEffectGA(Effect effect, List<CharacterView> targets = null, CharacterView caster = null)
    {
        Effect = effect;
        Targets = targets;
        Caster = caster;
    }
}
