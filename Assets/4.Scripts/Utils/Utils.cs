using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public static class Utils
{
    public static void WarmUp()
    {
        WarmUpShuffle();
    }

    public static void WarmUpShuffle()
    {
        var tmpList = new List<int> { 1, 2, 3 };
        tmpList.Shuffle();       // IList<int> 제네릭 인스턴스 준비
        var tmpArr = new[] { 1, 2, 3 };
        tmpArr.Shuffle();        // T[] 제네릭 인스턴스 준비
    }

    // List<T> 셔플
    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 배열 셔플
    public static void Shuffle<T>(this T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // 2차원 배열 셔플
    public static void Shuffle2D<T>(this T[,] array)
    {
        int height = array.GetLength(0);
        int width = array.GetLength(1);

        int total = height * width;

        for (int i = total - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            // i → (y1, x1), j → (y2, x2)
            int y1 = i / width;
            int x1 = i % width;

            int y2 = j / width;
            int x2 = j % width;

            // swap
            T temp = array[y1, x1];
            array[y1, x1] = array[y2, x2];
            array[y2, x2] = temp;
        }
    }

    // 리스트에서 임의의 요소 꺼내기
    public static T GetRandomElement<T>(this List<T> list, IEnumerable<T> excepts = null)
    {
        if (list == null || list.Count == 0)
        {
            return default;
        }

        if (excepts == null)
        {
            int randomIndex = Random.Range(0, list.Count);
            T randomValue = list[randomIndex];
            return randomValue;
        }
        else
        {
            HashSet<T> exceptsHashSet = new HashSet<T>(excepts);

            List<T> result = new List<T>(list);
            foreach (T except in exceptsHashSet)
            {
                result.Remove(except);
            }

            if (result.Count == 0)
            {
                return default;
            }
            else
            {
                int randomIndex = Random.Range(0, result.Count);
                T randomValue = result[randomIndex];
                return randomValue;
            }
        }
    }

    // 배열에서 임의의 요소 꺼내기
    public static T GetRandomElement<T>(this T[] array, IEnumerable<T> excepts = null)
    {
        return array.ToList().GetRandomElement(excepts);
    }

    // IReadOnlyList에서 임의의 요소 꺼내기
    public static T GetRandomElement<T>(this IReadOnlyList<T> list, IEnumerable<T> excepts = null)
    {
        return list.ToList().GetRandomElement(excepts);
    }

    /// <summary>
    /// 가중치(확률) 리스트를 기반으로 아이템 하나를 랜덤하게 선택하여 반환합니다.
    /// </summary>
    /// <param name="list">아이템 리스트</param>
    /// <param name="weights">각 아이템에 대응하는 가중치(확률) 리스트 (list와 길이가 같아야 함)</param>
    public static T PickWeighted<T>(this IList<T> list, IList<float> weights)
    {
        // 1. 방어 코드: 리스트가 비어있거나, 두 리스트의 길이가 다르면 기본값 반환
        if (list == null || list.Count == 0 || weights == null || weights.Count != list.Count)
        {
            Debug.LogError("PickWeighted Error: 리스트가 비어있거나 가중치 리스트와 길이가 다릅니다.");
            return default(T);
        }

        // 2. 가중치 총합 계산
        float totalWeight = 0f;
        foreach (float w in weights)
        {
            totalWeight += w;
        }

        // 3. 0 ~ 총 가중치 사이의 랜덤 값 생성
        float randomValue = Random.Range(0f, totalWeight);

        // 4. 누적 가중치(Cumulative Weight) 방식으로 선택
        float currentSum = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            currentSum += weights[i];

            // 랜덤 값이 현재 누적 구간에 포함되면 해당 인덱스의 아이템 반환
            if (randomValue <= currentSum)
            {
                return list[i];
            }
        }

        // 5. 부동소수점 오차 등으로 인해 루프를 빠져나온 경우 마지막 아이템 반환 (Safe Fallback)
        return list[list.Count - 1];
    }

    // 내 자식 다 삭제하기
    public static void DestroyAllChildren(this Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child == parent)
            {
                continue;
            }

            GameObject.Destroy(child.gameObject);
        }
    }

    public static T GetRandomEnumValue<T>() where T : System.Enum
    {
        var values = System.Enum.GetValues(typeof(T));
        return (T)values.GetValue(Random.Range(0, values.Length));
    }

    /// <summary>
    /// 자신을 포함한 모든 자식 오브젝트의 레이어를 변경합니다.
    /// </summary>
    /// <param name="obj">대상 오브젝트</param>
    /// <param name="layerIndex">변경할 레이어의 인덱스 (int)</param>
    public static void SetLayerRecursively(this GameObject obj, int layerIndex)
    {
        if (obj == null) return;

        obj.layer = layerIndex;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layerIndex);
        }
    }

    // keywordType에 맞는 키워드 중 무작위 하나 반환하는 메소드
    public static SO_KeywordData GetRandomKeywordData(EKeywordType keywordType)
    {
        List<SO_KeywordData> temp = new List<SO_KeywordData>();

        if((keywordType | EKeywordType.Subject) != 0)
        {
            temp.Add(DataManager.Instance.AllSubjectKeywords[Random.Range(0, DataManager.Instance.AllSubjectKeywords.Count)]);
        }

        if ((keywordType | EKeywordType.Adverb) != 0)
        {
            temp.Add(DataManager.Instance.AllAdverbKeywords[Random.Range(0, DataManager.Instance.AllAdverbKeywords.Count)]);
        }

        if ((keywordType | EKeywordType.Verb) != 0)
        {
            temp.Add(DataManager.Instance.AllVerbKeywords[Random.Range(0, DataManager.Instance.AllVerbKeywords.Count)]);
        }

        if ((keywordType | EKeywordType.Curse) != 0)
        {
            temp.Add(DataManager.Instance.AllCurseKeywords[Random.Range(0, DataManager.Instance.AllCurseKeywords.Count)]);
        }

        if(temp.Count > 0)
        {
            return temp[Random.Range(0, temp.Count)];
        }
        else
        {
            return null;
        }
    }

    public static Color GetKeywordColor(EKeywordType keywordType)
    {
        switch (keywordType)
        {
            case EKeywordType.Subject:
                return StyleManager.Instance.GetColor(EColorKey.키워드_주어);
            case EKeywordType.Adverb:
                return StyleManager.Instance.GetColor(EColorKey.키워드_부사);
            case EKeywordType.Verb:
                return StyleManager.Instance.GetColor(EColorKey.키워드_동사);
            case EKeywordType.Curse:
                return StyleManager.Instance.GetColor(EColorKey.키워드_저주);
        }

        return StyleManager.Instance.GetColor(EColorKey.White);
    }

    public static EKeyword GetRandomOwnedKeyword(EKeywordType type)
    {
        if (type == EKeywordType.Subject)
        {
            return EKeyword.None;
        }

        List<EKeyword> targetList = null;

        var gameModel = DataManager.Instance.GameModel;
        if (gameModel == null) return EKeyword.None;

        switch (type)
        {
            case EKeywordType.Adverb:
                targetList = gameModel.AdverbKeywords;
                break;
            case EKeywordType.Verb:
                targetList = gameModel.VerbKeywords;
                break;
            case EKeywordType.Curse:
                targetList = gameModel.CurseKeywords;
                break;
        }

        if (targetList != null && targetList.Count > 0)
        {
            return targetList.GetRandomElement ();
        }

        return EKeyword.None;
    }

    public static List<SO_KeywordData> GetFullPoolByType(EKeywordType type)
    {
        if ((type & EKeywordType.Adverb) != 0)
        {
            return DataManager.Instance.AllAdverbKeywords.ToList ();
        }
        else if ((type & EKeywordType.Verb) != 0)
        {
            return DataManager.Instance.AllVerbKeywords.ToList ();
        }

        return new List<SO_KeywordData> ();
    }

    public static bool CanUpgrade(SO_KeywordData currentData)
    {
        if (currentData == null) return false;

        int targetRank = currentData.Rank + 1;
        List<SO_KeywordData> pool = GetFullPoolByType (currentData.KeywordType); 

        if (pool == null || pool.Count == 0) return false;

        foreach (var keyword in pool)
        {
            if (keyword.Rank == targetRank) return true;
        }

        return false; 
    }

    public static void InvokeMethod(System.Action action, int delay)
    {
        AsyncDelayMethod(action, delay).Forget();
    }

    private static async UniTaskVoid AsyncDelayMethod(System.Action action, int delay)
    {
        await UniTask.Delay(delay);

        action?.Invoke();
    }
}
