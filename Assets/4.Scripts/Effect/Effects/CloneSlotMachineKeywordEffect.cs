using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

public class CloneSlotMachineKeywordEffect : Effect, ISelectionResolver
{
    [SerializeReference, SR] private SelectionResolver _selectionResolver;

    public SelectionResolver SelectionResolver => _selectionResolver;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        List<EKeyword> slotMachineKeywords = new List<EKeyword>();
        slotMachineKeywords.Add(SlotMachineSystem.Instance.GetSlotMachineResultKeyword(SelectionResolver.SelectedIndex[0]));
        slotMachineKeywords.Add(SlotMachineSystem.Instance.GetSlotMachineResultKeyword(SelectionResolver.SelectedIndex[0]));

        return new ChangeSlotMachineKeywordGA(slotMachineKeywords, SelectionResolver.SelectedIndex);
    }
}
