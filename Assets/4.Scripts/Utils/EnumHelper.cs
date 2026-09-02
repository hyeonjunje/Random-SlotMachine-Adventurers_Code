using System;

public static class EnumHelper<T> where T : struct, Enum
{
    private static readonly T[] _values;

    // 'None' (보통 0번 인덱스나 특정 값)을 제외한 값들만 따로 캐싱
    private static readonly T[] _validValues;

    static EnumHelper()
    {
        _values = (T[])Enum.GetValues(typeof(T));

        if (_values.Length > 1)
        {
            _validValues = new T[_values.Length - 1];
            Array.Copy(_values, 1, _validValues, 0, _values.Length - 1);
        }
        else
        {
            _validValues = Array.Empty<T>();
        }
    }

    // 0번 인덱스(None)를 제외한 랜덤 값 반환 (GC 0)
    public static T GetRandomValueExcludingNone()
    {
        if (_validValues.Length == 0) return default;

        int index = UnityEngine.Random.Range(0, _validValues.Length);
        return _validValues[index];
    }

    // 0번 인덱스(None)를 제외한 배열 반환
    public static T[] GetValues() => _validValues;
}