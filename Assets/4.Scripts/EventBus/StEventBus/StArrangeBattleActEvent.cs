using System.Collections.Generic;

public readonly struct StArrangeBattleActEvent
{
    public readonly CharacterView Caster;
    public readonly List<CharacterView> Targets;

    public StArrangeBattleActEvent(CharacterView caster, List<CharacterView> targets)
    {
        Caster = caster;
        Targets = targets;
    }
}
