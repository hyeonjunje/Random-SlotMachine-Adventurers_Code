using System.Collections.Generic;
using UnityEngine;

public class LevelUpPartyEffect : Effect
{
    [SerializeField] private int _levelDiff = 1;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new LevelUpPartyGA(_levelDiff, 0);
    }
}
