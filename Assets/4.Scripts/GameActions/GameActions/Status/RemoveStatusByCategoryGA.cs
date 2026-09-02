using System.Collections.Generic;

public class RemoveStatusByCategoryGA : GameAction
{
    public EStatusCategory StatusCategory { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public CharacterView Caster { get; private set; }
    public int RemoveCount { get; private set; }

    public RemoveStatusByCategoryGA(EStatusCategory statusCategory, List<CharacterView> targets, CharacterView caster, int count)
    {
        StatusCategory = statusCategory;
        Targets = new List<CharacterView>(targets);
        Caster = caster;
        RemoveCount = count;
    }
}
