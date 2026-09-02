using System.Collections.Generic;

// 중독용 딜데미지 GameAction
public class DealDamage_PoisionGA : GameAction, ICameraControllableGA
{
    public List<CharacterView> Targets { get; private set; }

    public ECameraActionType CameraActionType => ECameraActionType.JustCameraShakeInBattle;

    public DealDamage_PoisionGA(List<CharacterView> targets)
    {
        Targets = new List<CharacterView>(targets);
    }
}
