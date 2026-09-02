using UnityEngine;

public class GhostView : CharacterView
{
    [SerializeField] private SO_CharacterData _ghostCharacterData;

    public void Init()
    {
        Ghost ghost = new Ghost(_ghostCharacterData);
        base.Init(ghost, new HealthController(ghost.GetStat(EStatType.MaxHp).Value), new StatusController());
    }

    public override void EndTurn()
    {
    }

    public override void HandleOnDead(CharacterView killer)
    {
    }

    public override void SetActiveHUD(bool flag)
    {
    }

    public override void StartTurn()
    {
    }

    public override void HoverCharacter(bool flag)
    {
    }

    public override void PlayActSFX(ECharacterAnimationType characterAnimationType)
    {
    }
}
