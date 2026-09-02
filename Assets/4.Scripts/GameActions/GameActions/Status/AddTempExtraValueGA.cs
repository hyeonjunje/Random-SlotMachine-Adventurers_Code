public class AddTempExtraValueGA : GameAction
{
    public EAdverbEffectTargetType TargetType { get; private set; }
    public float ExtraValue { get; private set; }

    public AddTempExtraValueGA(EAdverbEffectTargetType targetType, float extraValue)
    {
        TargetType = targetType;
        ExtraValue = extraValue;
    }
}
