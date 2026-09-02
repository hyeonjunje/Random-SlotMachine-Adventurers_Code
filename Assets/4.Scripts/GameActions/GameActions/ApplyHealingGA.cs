using System.Collections.Generic;

public class ApplyHealingGA : GameAction
{
    public CharacterView Caster { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public HealingFormula HealingFormula { get; private set; }

    public ApplyHealingGA(CharacterView caster, List<CharacterView> targets, HealingFormula healingFormula)
    {
        Caster = caster;
        Targets = new List<CharacterView>(targets);
        HealingFormula = new HealingFormula(healingFormula);
    }
}