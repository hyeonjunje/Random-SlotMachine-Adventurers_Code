using System.Collections.Generic;

// 응징용 딜데미지 GameAction
public class DealDamage_PunishmentGA : GameAction, ICameraControllableGA
{
    public List<CharacterView> Targets { get; private set; }
    public ECharacterAnimationType CharacterAnimationType { get; private set; }

    public ECameraActionType CameraActionType => Targets[0].Character.BattleSideType == EBattleSideType.OurSide ? ECameraActionType.EnemyAttack : ECameraActionType.PlayerAttack;


    public DealDamage_PunishmentGA(List<CharacterView> targets, ECharacterAnimationType characterAnimationType)
    {
        Targets = new List<CharacterView>(targets);
        CharacterAnimationType = characterAnimationType;
    }
}
