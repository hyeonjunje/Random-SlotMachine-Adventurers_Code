public class PerformEventChoiceEffectGA : GameAction
{
    public ChoiceData ChoiceData { get; private set; }

    public PerformEventChoiceEffectGA(ChoiceData choiceData)
    {
        ChoiceData = choiceData;
    }
}