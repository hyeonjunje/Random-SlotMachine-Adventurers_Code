using Spine.Unity;
using UnityEngine;

public class PlayerView : CharacterView
{
    [SerializeField] private PlayerHUD _playerHUD;

    public Player Player { get; private set; }

    public void Init(Player player, HealthController healthController, StatusController statusController)
    {
        Player = player;

        base.Init(player, healthController, statusController);

        _playerHUD.Setup(this, player, Collider);

        AnimationController.Setup(GetComponentInChildren<SkeletonAnimation>());

        healthController.OnDealDamage += HandleOnDealDamage;
        healthController.OnRestoreHealth += HandleOnRestoreHealth;
        healthController.OnDead += HandleOnDead;
    }

    private void OnDestroy()
    {
        Player.Release();
        _playerHUD.Release();
    }

    public override void HandleOnDead(CharacterView killer)
    {
        if (Character?.HealthController == null || Character.HealthController.IsDead == false)
        {
            return;
        }

        if (ArtifactRuntimeState.TryConsumePartyRevive(out float reviveRatio))
        {
            int reviveHp = Mathf.Max(1, Mathf.RoundToInt(Character.HealthController.MaxHp * reviveRatio));
            Character.HealthController.SetCurrentHp(reviveHp);
            EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_PLAYERVIEW_069"), EMessageType.Notice));
            return;
        }

        UIManager.Instance.Open(EUIType.UI_Ending);
        UI_Ending uiEnding = UIManager.Instance.Get<UI_Ending>(EUIType.UI_Ending);
        uiEnding.SetEndindType(EEndingType.Defeat);
    }

    public override void EndTurn()
    {
        base.EndTurn();
        Character.EndTurn();
    }

    public void PrepareBattle()
    {
        Player.PrepareBattle();
    }

    public override void StartTurn()
    {
        Character.StartTurn();
    }

    public override void SetActiveHUD(bool flag)
    {
        _playerHUD.gameObject.SetActive(flag);
    }

    public override void HoverCharacter(bool flag)
    {
        _playerHUD.HoverCharacer(flag);
    }

    public override void PlayActSFX(ECharacterAnimationType characterAnimationType)
    {
        if (characterAnimationType == ECharacterAnimationType.Attack)
        {
            ESfxId attackSFX = ESfxId.Warrior_DefaultAttack1;

            switch (Player.PlayerData.PlayerJob)
            {
                case EPlayerJob.Warrior:
                    attackSFX = Random.Range(0, 2) == 0 ? ESfxId.Warrior_DefaultAttack1 : ESfxId.Warrior_DefaultAttack2;
                    break;
                case EPlayerJob.Dwarf:
                    attackSFX = Random.Range(0, 2) == 0 ? ESfxId.Dwarf_DefaultAttack1 : ESfxId.Dwarf_DefaultAttack2;
                    break;
                case EPlayerJob.Archer:
                    attackSFX = Random.Range(0, 2) == 0 ? ESfxId.Archer_DefaultAttack1 : ESfxId.Archer_DefaultAttack2;
                    break;
                case EPlayerJob.Priest:
                    attackSFX = Random.Range(0, 2) == 0 ? ESfxId.Priest_DefaultAttack1 : ESfxId.Priest_DefaultAttack2;
                    break;
                case EPlayerJob.Rogue:
                    attackSFX = Random.Range(0, 2) == 0 ? ESfxId.Rogue_DefaultAttack1 : ESfxId.Rogue_DefaultAttack2;
                    break;
            }

            AudioManager.Instance.PlaySFX(attackSFX);
        }
        else if (characterAnimationType == ECharacterAnimationType.Buff)
        {
            ESfxId buffSFX = Random.Range(0, 2) == 0 ? ESfxId.Buff1 : ESfxId.Buff2;
            AudioManager.Instance.PlaySFX(buffSFX);
        }
    }

    public void EndBattle()
    {
        for (EStatusType statusType = 0; statusType < EStatusType.Max; ++statusType)
        {
            RemoveStatus(null, statusType);
        }
    }

    private void HandleOnRestoreHealth(int prevHp, int currentHp)
    {

    }

    private void HandleOnDealDamage(int prevHp, int currentHp)
    {
        // 피해를 입을 때만 Hit Animation을 해준다.
        if (prevHp > currentHp)
        {
            SetAnimation(ECharacterAnimationType.Hit);
        }
    }
}

