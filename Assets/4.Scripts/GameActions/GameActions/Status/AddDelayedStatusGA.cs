using System.Collections.Generic;

public class AddDelayedStatusGA : GameAction
{
    public List<CharacterView> Targets { get; private set; }
    public EStatusType StatusType { get; private set; }
    public int Value { get; private set; }

    public AddDelayedStatusGA(List<CharacterView> targets, EStatusType statusType, int value)
    {
        Targets = new List<CharacterView>(targets);
        StatusType = statusType;
        Value = value;
    }
}
