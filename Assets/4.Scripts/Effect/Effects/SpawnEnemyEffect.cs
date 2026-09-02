using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemyEffect : Effect
{
    [SerializeField] private SO_EnemyData _enemyData;
    [SerializeField] private int _posIndex;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        Enemy enemy = new Enemy(_enemyData);

        return new SpawnEnemyGA(enemy, _posIndex);
    }
}
