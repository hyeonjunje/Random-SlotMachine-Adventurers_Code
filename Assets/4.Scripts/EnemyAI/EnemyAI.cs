using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI
{
    private Dictionary<int, EnemyActGroup> _actGroups = new Dictionary<int, EnemyActGroup>();
    private Dictionary<EnemyAct, int> _actRepeatCount = new Dictionary<EnemyAct, int>();

    public EnemyActGroup CurrentEnemyActGroup { get; private set; }
    public EnemyAct CurrentAct { get; private set; } = null;

    public int ActCount { get; private set; } = 0;
    public bool IsAct { get; set; } = false;

    public event Action<int, bool> OnChangeActCount; // 행동 카운트, 행동 유무

    public EnemyAI(EnemyView enemyView, SO_EnemyAI enemyAIData)
    {
        foreach (EnemyActGroup actGroup in enemyAIData.EnemyActGroup)
        {
            _actGroups[actGroup.Id] = actGroup;

            foreach (EnemyActTransition enemyActTransition in actGroup.EnemyActTransitions)
            {
                if (enemyActTransition.Condition != null)
                {
                    enemyActTransition.Condition.SetOwner(enemyView);
                }
            }

        }
    }

    public void Release()
    {
        _actGroups.Clear();
        _actRepeatCount.Clear();
        CurrentEnemyActGroup = null;
        CurrentAct = null;
    }

    public void ChangeActCount(int diff)
    {
        ActCount = Mathf.Max(ActCount + diff, 0);
        OnChangeActCount?.Invoke(ActCount, IsAct);
    }

    public void SetActCount(int value)
    {
        ActCount = Mathf.Max(value, 0);
        OnChangeActCount?.Invoke(ActCount, IsAct);
    }

    public void NextEnemyAct()
    {
        List<EnemyAct> nextActs = new List<EnemyAct>();
        List<float> nextActProbability = new List<float>();

        // 처음
        if (CurrentEnemyActGroup == null)
        {
            foreach (EnemyActGroup actGroup in _actGroups.Values)
            {
                if(actGroup.IsStart)
                {
                    CurrentEnemyActGroup = actGroup;
                    _actRepeatCount.Clear();
                    foreach (EnemyAct enemyAct in CurrentEnemyActGroup.Acts)
                    {
                        _actRepeatCount[enemyAct] = 0;
                    }
                }
            }
        }
        else
        {
            bool isGroupChange = false;

            foreach (EnemyActTransition enemyActTransition in CurrentEnemyActGroup.EnemyActTransitions)
            {
                // 전이 조건이 충족되면 그 순간 그 행동으로 Set
                if (enemyActTransition.Condition != null && enemyActTransition.Condition.SubConditionIsMet(null))
                {
                    if(_actGroups.ContainsKey(enemyActTransition.NextId))
                    {
                        isGroupChange = true;
                        if (CurrentEnemyActGroup != _actGroups[enemyActTransition.NextId])
                        {
                            CurrentEnemyActGroup = _actGroups[enemyActTransition.NextId];
                            _actRepeatCount.Clear();
                            foreach (EnemyAct enemyAct in CurrentEnemyActGroup.Acts)
                            {
                                _actRepeatCount[enemyAct] = 0;
                            }
                        }
                        
                        break;
                    }
                }
            }

            // 조건에 의해 전환이 안되면 그냥 다음 그룹으로 간다.
            if(isGroupChange == false)
            {
                if(_actGroups.ContainsKey(CurrentEnemyActGroup.NextId))
                {
                    if(CurrentEnemyActGroup != _actGroups[CurrentEnemyActGroup.NextId])
                    {
                        CurrentEnemyActGroup = _actGroups[CurrentEnemyActGroup.NextId];
                        _actRepeatCount.Clear();
                        foreach (EnemyAct enemyAct in CurrentEnemyActGroup.Acts)
                        {
                            _actRepeatCount[enemyAct] = 0;
                        }
                    }
                }
            }
        }

        // 그룹에서 확률에 따라 행동 결정
        foreach(EnemyAct enemyAct in CurrentEnemyActGroup.Acts)
        {
            nextActs.Add(enemyAct);
            nextActProbability.Add(enemyAct.Probability);
        }

        int tempCount = 0;

        // 각 행동의 반복 카운트 생각
        while(true)
        {
            // 무한 반복이라 100이라는 한계를 줌
            if(tempCount++ > 100)
            {
                Debug.LogError("무한 반복 발생");
                break;
            }

            CurrentAct = nextActs.PickWeighted(nextActProbability);

            // 반복카운트가 아직 안넘었으면 해당 행동으로 결정
            if(CurrentAct.RepeatLimit == -1 || _actRepeatCount[CurrentAct] <= CurrentAct.RepeatLimit)
            {
                // 선택됐으니 다른 행동들은 다 0으로 초기화
                foreach(EnemyAct enemyAct in CurrentEnemyActGroup.Acts)
                {
                    if(enemyAct != CurrentAct)
                    {
                        _actRepeatCount[enemyAct] = 0;
                    }
                }
                _actRepeatCount[CurrentAct]++;
                break;
            }
        }

        IsAct = false;
        ActCount = 0;
        ChangeActCount(CurrentAct.ActCount);
    }
}
