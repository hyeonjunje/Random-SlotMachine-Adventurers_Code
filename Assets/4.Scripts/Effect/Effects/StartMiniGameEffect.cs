using System.Collections.Generic;
using UnityEngine;

public class StartMiniGameEffect : Effect
{
    [SerializeField] private EMiniGameType _miniGameType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new StartMiniGameGA(_miniGameType);
    }
}
