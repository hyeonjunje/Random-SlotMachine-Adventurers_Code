public enum ESlotMachineLineDirection
{
    Horizontal,
    Vertical,
}

public class RerollSlotMachineLineGA : GameAction
{
    public ESlotMachineLineDirection Direction { get; private set; }
    public int LineCount { get; private set; }

    public RerollSlotMachineLineGA(ESlotMachineLineDirection direction, int lineCount)
    {
        Direction = direction;
        LineCount = lineCount;
    }
}
