using System.Collections;
using UnityEngine;

public class Keyword
{
    public SO_KeywordData KeywordData { get; private set; }
    public int SlotIndex { get; private set; }

    public Keyword(SO_KeywordData keywordData, int slotIndex)
    {
        KeywordData = keywordData;
        SlotIndex = slotIndex;
    }
}
