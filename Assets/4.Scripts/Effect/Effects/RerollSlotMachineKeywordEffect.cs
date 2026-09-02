using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

public class RerollSlotMachineKeywordEffect : Effect, ISelectionResolver
{
    [SerializeReference, SR] private SelectionResolver _selectionResolver;

    public SelectionResolver SelectionResolver => _selectionResolver;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        if (ArtifactRuntimeState.IsRerollDisabled)
        {
            return new BlockedRerollGA(ArtifactRuntimeState.GetAdjustedSlotClickRerollManaCost(1));
        }

        int slotIndex = 0;

        if (SelectionResolver.SelectedIndex.Count != 0)
        {
            slotIndex = SelectionResolver.SelectedIndex[0];
        }

        return new RerollSlotMachineKeywordGA(slotIndex);
    }
}
