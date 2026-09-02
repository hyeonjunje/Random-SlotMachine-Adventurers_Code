using System.Collections.Generic;

public class ClearNodeEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new ClearNodeGA();
    }
}
