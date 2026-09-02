public class ApplyAbilityGA : GameAction
{
    public SO_AbilityData AbilityData { get; private set; }

    public CharacterView Owner { get; private set; }

    public ApplyAbilityGA(SO_AbilityData abilityData, CharacterView owner)
    {
        AbilityData = abilityData;
        Owner = owner;
    }
}
