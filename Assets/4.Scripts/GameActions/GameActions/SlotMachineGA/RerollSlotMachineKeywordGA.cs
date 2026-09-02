public class RerollSlotMachineKeywordGA : GameAction
{
    public int SlotIndex { get; private set; }

    public RerollSlotMachineKeywordGA(int slotIndex)
    {
        SlotIndex = slotIndex;
    }
}

public class BlockedRerollGA : GameAction
{
    public int RefundMana { get; private set; }

    public BlockedRerollGA(int refundMana)
    {
        RefundMana = refundMana;
    }
}
