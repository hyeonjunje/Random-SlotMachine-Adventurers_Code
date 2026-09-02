using System.Collections.Generic;

/// <summary>
/// 제네릭 CSV 파서 유틸리티.
/// 헤더 행을 키로 사용해 각 행을 Dictionary로 파싱하고, 특정 컬럼 기준 그룹핑을 지원합니다.
/// </summary>
public static class CSVParser
{
    /// <summary>
    /// CSV 텍스트를 파싱하여 각 행을 Dictionary로 반환합니다.
    /// 첫 번째 행을 헤더(키)로 사용합니다.
    /// </summary>
    public static List<Dictionary<string, string>> Parse(string csvText)
    {
        var result = new List<Dictionary<string, string>>();
        if (string.IsNullOrEmpty(csvText)) return result;

        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        if (lines.Length <= 1) return result;

        // 헤더 파싱
        string[] headers = SplitCSVLine(lines[0]);
        for (int i = 0; i < headers.Length; i++)
            headers[i] = headers[i].Trim();

        // 데이터 행 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = SplitCSVLine(line);
            var row = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length; j++)
            {
                string value = j < values.Length ? values[j].Trim() : "";
                row[headers[j]] = value;
            }

            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// 파싱된 행들을 특정 컬럼 기준으로 그룹핑합니다.
    /// </summary>
    public static Dictionary<string, List<Dictionary<string, string>>> GroupBy(
        List<Dictionary<string, string>> rows, string keyColumn)
    {
        var groups = new Dictionary<string, List<Dictionary<string, string>>>();

        foreach (var row in rows)
        {
            if (!row.TryGetValue(keyColumn, out string key)) continue;

            if (!groups.ContainsKey(key))
                groups[key] = new List<Dictionary<string, string>>();

            groups[key].Add(row);
        }

        return groups;
    }

    /// <summary>
    /// 그룹핑된 데이터를 다시 서브 그룹으로 분할합니다.
    /// 예: Floor 그룹 안에서 BundleIndex로 다시 그룹핑
    /// </summary>
    public static SortedDictionary<int, List<Dictionary<string, string>>> SubGroupByInt(
        List<Dictionary<string, string>> rows, string keyColumn)
    {
        var subGroups = new SortedDictionary<int, List<Dictionary<string, string>>>();

        foreach (var row in rows)
        {
            if (!row.TryGetValue(keyColumn, out string keyStr)) continue;
            if (!int.TryParse(keyStr, out int key)) continue;

            if (!subGroups.ContainsKey(key))
                subGroups[key] = new List<Dictionary<string, string>>();

            subGroups[key].Add(row);
        }

        return subGroups;
    }

    private static string[] SplitCSVLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 이스케이프된 따옴표("") 처리
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false; // 따옴표 닫기
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true; // 따옴표 열기
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
