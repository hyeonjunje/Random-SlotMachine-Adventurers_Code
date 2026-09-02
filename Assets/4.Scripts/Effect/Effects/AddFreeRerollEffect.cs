using System.Collections.Generic;

public class AddFreeRerollEffect : Effect
{
    public int Amount = 1;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new AddFreeRerollGA (Amount);
    }
}
