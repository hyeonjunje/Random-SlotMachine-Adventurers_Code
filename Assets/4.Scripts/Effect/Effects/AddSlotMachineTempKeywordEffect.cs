using System.Collections.Generic;
using UnityEngine;

public class AddSlotMachineTempKeywordEffect : Effect
{
    [SerializeField] private EKeyword _keyword;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new AddSlotMachineTempKeywordGA(_keyword);
    }
}
