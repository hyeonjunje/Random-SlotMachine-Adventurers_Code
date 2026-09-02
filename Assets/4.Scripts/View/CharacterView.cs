using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private Transform _characterPrefabParent;
    [field: SerializeField] public BoxCollider2D Collider { get; private set; }
    [field: SerializeField] public CharacterAnimationController AnimationController { get; private set; }
    public Character Character { get; private set; }

    public virtual void Init(Character character, HealthController healthController, StatusController statusController)
    {
        Character = character;
        Character.SetHealthController(healthController);
        Character.SetStatusController(statusController);

        _characterPrefabParent.DestroyAllChildren();
        GameObject characterInstance = Instantiate(Character.CharacterData.CharacterPrefab, _characterPrefabParent);

        Collider.offset = Character.CharacterData.ColliderOffset;
        Collider.size = Character.CharacterData.ColliderSize;

        AddAbility();
    }

    public void AddAbility()
    {
        if (Character.CharacterData.AbilityData != null)
        {
            ApplyAbilityGA applyAbilityGA = new ApplyAbilityGA(Character.CharacterData.AbilityData, this);
            ActionSystem.Instance.AddReaction(applyAbilityGA);
        }
    }

    public void DealDamage(CharacterView caster, DamageFormula damageFormula)
    {
        Character.DealDamage(caster, damageFormula);
    }

    public void DealDamage(int damage)
    {
        Character.DealDamage(damage);
    }

    public void RestoreHealth(CharacterView caster, HealingFormula healingFormula)
    {
        Character.RestoreHealth(caster, healingFormula);
    }

    public void AddShield(CharacterView caster, ShieldFormula sheidlFormula)
    {
        Character.AddShield(caster, sheidlFormula);
    }

    public void AddStatus(CharacterView caster, Status status)
    {
        Character.AddStatus(status);
    }

    public void RemoveStatus(CharacterView caster, EStatusType statusType)
    {
        Character.ReleaseStatus(statusType);
    }

    public void UpdateStatus(Status status)
    {
        Character.UpdateStatus(status);
    }

    public void SetAnimation(ECharacterAnimationType animationType)
    {
        AnimationController.PlayAnimation(animationType);
    }

    public Vector3 GetPositionCenter()
    {
        Vector3 offset = Character.CharacterData.ColliderOffset + UnityEngine.Random.insideUnitCircle * 0.1f;
        return transform.position + offset;
    }

    public abstract void HandleOnDead(CharacterView killer);

    public abstract void StartTurn();

    public abstract void SetActiveHUD(bool flag);

    public abstract void HoverCharacter(bool flag);

    public abstract void PlayActSFX(ECharacterAnimationType characterAnimationType);

    public virtual void EndTurn()
    {
        foreach(Status status in Character.StatusController.Statuses.Values)
        {
            if(status.IsSingleTurn)
            {
                RemoveStatusGA removeStatusGA = new RemoveStatusGA(status, new List<CharacterView> { this }, null);
                ActionSystem.Instance.AddReaction(removeStatusGA);
            }
        }
    }
}
