using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AddStatusEffect : Effect
{
    [SerializeField] private SO_StatusData _status;
    [SerializeField] private int _turn;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new AddStatusGA(_status, _turn, targets, caster);
    }
}
