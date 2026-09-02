using System.Collections.Generic;

public class AddShieldGA : GameAction
{
    public CharacterView Caster { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public ShieldFormula ShieldFormula { get; private set; }

    public AddShieldGA(CharacterView caster, List<CharacterView> targets, ShieldFormula shieldFormula)
    {
        Caster = caster;
        Targets = new List<CharacterView>(targets);
        ShieldFormula = new ShieldFormula(shieldFormula);
    }
}
