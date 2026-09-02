using System;
using System.Collections.Generic;
using UnityEngine;
public class Player : Character
{
    public SO_PlayerData PlayerData { get; private set; }

    public int Level { get; protected set; }
    public bool IsMaxLevel => Level >= GameDefine.MAX_LEVEL;
    public Player(SO_PlayerData playerData) : base(playerData, EBattleSideType.OurSide)
    {
        PlayerData = playerData;

        Level = 1;
    }

    public override void Release()
    {
        base.Release();
    }

    public void Merge(Player sourcePlayer)
    {
        LevelUp (1);
    }

    public int LevelUp(int levels = 1) // 레벨 & 능력치 증가
    {
        int actualLevels = Mathf.Max (0, Mathf.Min (levels, GameDefine.MAX_LEVEL - Level));

        for (int i = 0; i < actualLevels; i++)
        {
            ApplyLevelUpIncrements(PlayerData.LevelUpIncrements);

            Level++;

            HealthController.ChangeMaxHp(PlayerData.LevelUpIncrements.maxHp);
        }

        if (actualLevels > 0)
        {
            OnDataChanged?.Invoke ();
        }

        return actualLevels;
    }

    private void ApplyLevelUpIncrements(SO_CharacterData.STStats increment)
    {
        GetStat(EStatType.MaxHp)?.AddBase(increment.maxHp);
        GetStat(EStatType.AttackPower)?.AddBase(increment.attackPower);
    }

    public override string GetName()
    {
        return LocalizationManager.Instance.Get(PlayerData.SubjectKeyword.ToString());
    }
    public void RestoreLevelDirect(int targetLevel)
    {
        targetLevel = Mathf.Clamp (targetLevel, 1, GameDefine.MAX_LEVEL);

        Level = 1;

        int diff = targetLevel - 1;
        for (int i = 0; i < diff; i++)
        {
            ApplyLevelUpIncrements (PlayerData.LevelUpIncrements);
            Level++;
        }

        OnDataChanged?.Invoke ();
    }

    public void PrepareBattle()
    {
        // 전투 시작 시 방어도 초기화 (보존 상태 있으면 보존)
        HealthController.ClearShield(false);
    }
}
