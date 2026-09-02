using System;
using UnityEngine;
using System.Collections.Generic;


[Serializable]
public class StartStageEffect : Effect
{
    [SerializeField] private int _stageIndex = 0;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new StartStageGA(_stageIndex);
    }
}