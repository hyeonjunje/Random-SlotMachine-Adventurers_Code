using System.Collections.Generic;

// 감전용 딜데미지 GameAction
public class DealDamage_ElectricGA : GameAction, ICameraControllableGA
{
    public List<CharacterView> Targets { get; private set; }

    public ECameraActionType CameraActionType => ECameraActionType.JustCameraShakeInBattle;

    public DealDamage_ElectricGA(List<CharacterView> targets)
    {
        Targets = new List<CharacterView>(targets);
    }
}
