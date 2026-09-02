using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택을 해결해서 결과를 만들어낸다
/// 카드 게임을 예를 들면 이 카드를 사용하면 패에 있는 카드 2장을 선택해 버립니다. 에서
/// 패에 있는 카드 2장을 선택하는 기능을 담당하는 클래스
/// 즉, 사용자의 선택을 받고 해당 index를 저장하는 클래스
/// </summary>
[System.Serializable]
public abstract class SelectionResolver
{
    [field:SerializeField] public string TransformName { get; private set; }
    [field: SerializeField] public int SelectCount { get; private set; }
    public List<int> SelectedIndex { get; private set; } = new List<int>();

    public virtual IEnumerator CoResolveSelection()
    {
        SelectedIndex.Clear();

        while (SelectedIndex.Count < SelectCount)
        {
            yield return null;
        }
    }

    public virtual void AddIndex(int index)
    {
        SelectedIndex.Add(index);
    }
}
