using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

public class ChangeSlotMachineKeywordEffect : Effect, ISelectionResolver
{
    [SerializeField] private EKeyword _keyword;
    [SerializeReference, SR] private SelectionResolver _selectionResolver;

    public SelectionResolver SelectionResolver => _selectionResolver;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        Player player = caster.Character as Player;

        if (player == null)
        {
            return null;
        }

        return new ChangeSlotMachineKeywordGA(new List<EKeyword> { _keyword }, SelectionResolver.SelectedIndex);
    }
}
