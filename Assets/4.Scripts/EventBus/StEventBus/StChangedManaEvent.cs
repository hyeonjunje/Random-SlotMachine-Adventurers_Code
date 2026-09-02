public readonly struct StChangedManaEvent
{
    public readonly float CurrentMana;
    public readonly float MaxMana;

    public StChangedManaEvent(float currentMana, float maxMana)
    {
        CurrentMana = currentMana;
        MaxMana = maxMana;
    }
}
