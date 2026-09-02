using System.Collections.Generic;

public class ChangeStatValueGA : GameAction
{
    public EStatType StatType { get; private set; }
    public EStatModType ModType { get; private set; }
    public float Value { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public CharacterView Caster { get; private set; }

    public ChangeStatValueGA(EStatType statType, EStatModType modType, float value, List<CharacterView> targets, CharacterView caster)
    {
        StatType = statType;
        ModType = modType;
        Value = value;
        if (targets != null)
        {
            Targets = new List<CharacterView> (targets);
        }
        else
        {
            Targets = new List<CharacterView> ();
        }
        Caster = caster;
    }

    public void MultiplyValue(float multiplier)
    {
        Value *= multiplier;
    }
}
