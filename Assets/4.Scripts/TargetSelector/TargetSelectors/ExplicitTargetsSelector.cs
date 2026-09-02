using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExplicitTargetsSelector : TargetSelector
{
    [SerializeField] private EBattleSideType _type;

    private CharacterView _target;

    public void SetTarget(CharacterView caster, CharacterView target)
    {
        if (caster == null || target == null)
        {
            _target = null;
            return;
        }

        var casterSide = caster.Character.BattleSideType;
        var targetSide = target.Character.BattleSideType;

        bool allowed = false;

        if (_type == EBattleSideType.OurSide)
        {
            allowed = (casterSide == targetSide);
        }
        else if(_type == EBattleSideType.EnemySide)
        {
            allowed = (casterSide != targetSide);
        }

        _target = allowed ? target : null;
    }

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        if (_target != null)
        {
            return new List<CharacterView> { _target };
        }

        return new List<CharacterView> ();
    }
}
