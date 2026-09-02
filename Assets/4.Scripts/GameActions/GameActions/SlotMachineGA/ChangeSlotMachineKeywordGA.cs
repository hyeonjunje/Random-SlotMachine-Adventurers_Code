using System.Collections.Generic;

public class ChangeSlotMachineKeywordGA : GameAction
{
    public List<EKeyword> SlotMachineKeywords { get; private set; } // 바꿀 키워드
    public List<int> SlotIndexes { get; private set; } // 바꿀 슬롯의 인덱스

    public ChangeSlotMachineKeywordGA(List<EKeyword> slotMachineKeywords, List<int> slotIndexes)
    {
        SlotMachineKeywords = new List<EKeyword>(slotMachineKeywords);
        SlotIndexes = new List<int>(slotIndexes);
    }
}
