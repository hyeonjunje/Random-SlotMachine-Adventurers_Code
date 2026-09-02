using System.Collections.Generic;
using UnityEngine;

// 다음 전투 때 _probability의 확률로 targets에게 _statusType 상태이상을 _value만큼 입히는 Effect
public class AddDelayedStatusEffect : Effect
{
    [SerializeField] private float _probability;
    [SerializeField] private EStatusType _statusType;
    [SerializeField] private int _value;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        float randomValue = Random.Range(0, 1f);

        if (randomValue <= _probability)
        {
            return new AddDelayedStatusGA(targets, _statusType, _value);
        }
        else
        {
            return new AddDelayedStatusGA(new List<CharacterView>(), _statusType, _value);
        }
    }
}
