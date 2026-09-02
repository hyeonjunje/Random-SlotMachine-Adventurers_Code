using System;
using System.Collections.Generic;

public class DealDamageGA : GameAction, ICameraControllableGA
{
    public CharacterView Caster { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public DamageFormula DamageFormula { get; private set; }
    public bool IsArtifactGenerated { get; private set; } = false;

    public ECameraActionType CameraActionType => Targets[0].Character.BattleSideType == EBattleSideType.OurSide ? ECameraActionType.EnemyAttack : ECameraActionType.PlayerAttack;

    public DealDamageGA(CharacterView caster, List<CharacterView> targets, DamageFormula damageFormula)
    {
        Caster = caster;
        Targets = new List<CharacterView>(targets);
        DamageFormula = new DamageFormula(damageFormula);
    }

    public void MarkArtifactGenerated()
    {
        IsArtifactGenerated = true;
    }
}
