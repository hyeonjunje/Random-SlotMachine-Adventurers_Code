using System.Collections;
public class DecreaseStatusGA : GameAction
{
    public Status Status { get; private set; }

    public DecreaseStatusGA(Status status)
    {
        Status = status;
    }
}
