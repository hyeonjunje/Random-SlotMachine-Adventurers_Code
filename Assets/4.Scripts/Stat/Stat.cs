using System.Collections.Generic;
using UnityEngine;

public class Stat
{
    public string StatName { get; private set; }

    public int BaseValue { get; private set; }
    public float AddValue { get; private set; }
    public float MulValue { get; private set; }
    public float FinalMulValue { get; private set; }

    private bool _dirty = true;
    private int _cached;

    public Stat(int baseValue, string statName) 
    {
        StatName = statName;
        BaseValue = baseValue;

        AddValue = 0;
        MulValue = 0;
        FinalMulValue = 0;

        _dirty = true;
    }

    public int Value
    {
        get
        {
            if (_dirty)
            {
                Recalculate();
            }
            return _cached;
        }
    }

    public void AddModifier(EStatModType statModType, float value)
    {
        switch (statModType)
        {
            case EStatModType.Add:
                AddValue += value;
                break;
            case EStatModType.Mul:
                MulValue += value;
                break;
            case EStatModType.FinalMul:
                FinalMulValue += value;
                break;
        }
        _dirty = true;
    }

    public void AddBase(int delta) // 캐릭터 합성시 스탯 증가
    {
        BaseValue += delta;
        _dirty = true;
    }

    private void Recalculate()
    {
        _cached = Mathf.RoundToInt((BaseValue + AddValue) * (1f + MulValue) * (1f + FinalMulValue));
        _dirty = false;
    }
}