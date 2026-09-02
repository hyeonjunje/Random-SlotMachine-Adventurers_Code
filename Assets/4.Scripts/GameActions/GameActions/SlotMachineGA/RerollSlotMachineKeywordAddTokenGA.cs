public class RerollSlotMachineKeywordAddTokenGA : GameAction
{
    public ESlotMachineRerollKeywordType SlotMachineRerollKeywordType { get; private set; }
    public EKeyword CausedKeyword { get; private set; }

    public RerollSlotMachineKeywordAddTokenGA(ESlotMachineRerollKeywordType slotMachineRerollKeywordType, EKeyword causedKeyword)
    {
        SlotMachineRerollKeywordType = slotMachineRerollKeywordType;
        CausedKeyword = causedKeyword;
    }
}