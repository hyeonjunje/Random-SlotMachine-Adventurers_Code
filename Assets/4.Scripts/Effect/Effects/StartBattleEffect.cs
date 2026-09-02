using System.Collections.Generic;
using UnityEngine;

public class StartBattleEffect : Effect
{
    [SerializeField] private SO_MatchupData _matchData;
    [SerializeField] private EMapNodeType _battleType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new PrepareBattleGA(_matchData.MatchupEnemyBundle, _battleType);
    }
}
