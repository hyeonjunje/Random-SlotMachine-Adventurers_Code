using System.Collections.Generic;

public class RemoveStatusGA : GameAction
{
    public Status Status { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public CharacterView Caster { get; private set; }

    public RemoveStatusGA(Status status, List<CharacterView> targets, CharacterView caster)
    {
        Status = status;
        Targets = new List<CharacterView>(targets);
        Caster = caster;
    }
}