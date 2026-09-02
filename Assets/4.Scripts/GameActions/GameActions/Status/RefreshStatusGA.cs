using System.Collections.Generic;

public class RefreshStatusGA : GameAction
{
    public Status Status { get; private set; }
    public List<CharacterView> Targets { get; private set; }

    public RefreshStatusGA(Status status, List<CharacterView> targets)
    {
        Status = status;
        Targets = new List<CharacterView>(targets);
    }
}