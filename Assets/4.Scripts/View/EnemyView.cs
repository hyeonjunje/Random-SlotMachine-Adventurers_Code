using System.Collections.Generic;
using UnityEngine;

public class EnemyView : CharacterView
{
    [SerializeField] private EnemyHUD _enemyHUD;

    public Enemy Enemy { get; private set; }

    public void Init(Enemy enemy, HealthController healthController, StatusController statusController)
    {
        Enemy = enemy;

        base.Init(enemy, healthController, statusController);
        Enemy.SetEnemyAI(this);
        _enemyHUD.Setup(this, enemy, healthController, statusController, Collider);

        healthController.OnDead += HandleOnDead;
        healthController.OnDealDamage += HandleOnDealDamage;
        healthController.OnRestoreHealth += HandleOnRestoreHealth;

        AnimationController.Setup(GetComponentInChildren<Animator>());
    }

    private void OnDestroy()
    {
        Enemy.Release();
        _enemyHUD.Release();
    }

    public override void HandleOnDead(CharacterView killer)
    {
        EnemyDeadGA characterDeadGA = new EnemyDeadGA(killer, this);
        ActionSystem.Instance.AddReaction(characterDeadGA);
    }

    public override void EndTurn()
    {
        base.EndTurn();
        Character.EndTurn();
    }

    public override void StartTurn()
    {
        Character.StartTurn();
    }

    public override void SetActiveHUD(bool flag)
    {
        _enemyHUD.gameObject.SetActive(flag);
    }

    public override void HoverCharacter(bool flag)
    {
        _enemyHUD.HoverCharacer(flag);
    }

    public override void PlayActSFX(ECharacterAnimationType characterAnimationType)
    {
        if(characterAnimationType == ECharacterAnimationType.Attack)
        {
            AudioManager.Instance.PlaySFX(ESfxId.Warrior_DefaultAttack1);
        }
        else if(characterAnimationType == ECharacterAnimationType.Buff)
        {
            ESfxId buffSFX = Random.Range(0, 2) == 0 ? ESfxId.Buff1 : ESfxId.Buff2;
            AudioManager.Instance.PlaySFX(buffSFX);
        }
    }

    private void HandleOnRestoreHealth(int prevHp, int currentHp)
    {

    }

    private void HandleOnDealDamage(int prevHp, int currentHp)
    {
        // 피해를 입을 때만 Hit Animation을 해준다.
        if(prevHp > currentHp)
        {
            SetAnimation(ECharacterAnimationType.Hit);
        }
    }

    private void OnMouseDown()
    {
        if(BattleSystem.Instance.BattleState == EBattleState.SelectTarget)
        {
            // 이미 포함되어있다면 빼준다.
            if (BattleSystem.Instance.CurrentTargets.Contains(this))
            {
                SetTarget(false);
            }
            else // 없었다면 넣어준다.
            {
                SetTarget(true);
            }
        }
    }

    public void SetTarget(bool flag)
    {
        List<EnemyView> enemyViews = BattleSystem.Instance.CurrentTargets;

        if (flag)
        {
            enemyViews.Add(this);
            _enemyHUD.SetActiveTarget(true, enemyViews.Count);
        }
        else
        {
            enemyViews.Remove(this);
            _enemyHUD.SetActiveTarget(false, 0);
        }

        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.UpdateTargets();
    }
}
