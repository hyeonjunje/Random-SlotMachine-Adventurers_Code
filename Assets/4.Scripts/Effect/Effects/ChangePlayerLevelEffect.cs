using System.Collections.Generic;
using UnityEngine;

public class ChangePlayerLevelEffect : Effect
{
    [SerializeField, Range (0, 99)] private int _levelDiff;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        PlayerView targetPlayer = null;

        foreach (CharacterView target in targets)
        {
            if (target is PlayerView playerView)
            {
                targetPlayer = playerView;
                break; 
            }
        }

        if (targetPlayer == null)
        {
            return null;
        }

        return new LevelUpPlayerGA (_levelDiff, targetPlayer, 0);
    }
}