using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum ELanguage
{
    KO,
    EN,
    None,
}

public class LocalizationManager : SingletonScene<LocalizationManager>
{
    [SerializeField] private TextAsset _localizationCsvFile;

    // Key -> (Language -> Text)
    private Dictionary<string, Dictionary<ELanguage, string>> _localizedText = new Dictionary<string, Dictionary<ELanguage, string>>();
    
    public ELanguage CurrentLanguage { get; private set; } = ELanguage.None;

    public event Action<ELanguage> OnLanguageChanged;

    protected override void OnAwakeSingleton()
    {
        LoadLocalizationData();
    }

    private void Start()
    {
        ChangeLanguage(SettingsManager.Instance.Language);
    }

    public void ChangeLanguage(ELanguage newLanguage)
    {
        if (CurrentLanguage == newLanguage)
            return;

        CurrentLanguage = newLanguage;
        OnLanguageChanged?.Invoke(CurrentLanguage);

        TextParser.Init();
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (_localizedText.TryGetValue(key, out var dict))
        {
            if (dict.TryGetValue(CurrentLanguage, out string text))
            {
                // 줄바꿈 문자를 실제 줄바꿈으로 치환
                return text.Replace("\\n", "\n");
            }
            // 영어가 비어있거나 찾을 수 없을 경우 한국어로 폴백
            if (dict.TryGetValue(ELanguage.KO, out string fallbackText))
            {
                return fallbackText.Replace("\\n", "\n");
            }
        }
        
        // 키를 찾을 수 없는 경우 키 값을 그대로 반환 (번역 누락 확인 용도)
        return key;
    }

    public void LoadLocalizationData()
    {
        _localizedText.Clear();
        
        if (_localizationCsvFile == null)
        {
            Debug.LogWarning("LocalizationData.csv 파일이 없습니다");
            return;
        }

        ParseCSV(_localizationCsvFile.text);
    }

    private void ParseCSV(string csvContent)
    {
        // 엑셀 저장 시 생성되는 큰따옴표가 포함된 CSV 파싱을 위한 정규식 (탭 구분자)
        string pattern = @"(((?<x>(?=[\t\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^\t\r\n]+))\t?)";
        
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        // 첫 번째 줄(Header: Key, KO, EN)은 건너뜁니다.
        for (int i = 1; i < lines.Length; i++)
        {
            List<string> row = new List<string>();
            MatchCollection matches = Regex.Matches(lines[i], pattern);

            if(matches.Count == 1)
            {
                continue;
            }

            foreach (Match match in matches)
            {
                string val = match.Groups["x"].Value;
                // 이스케이프된 큰따옴표 복구
                val = val.Replace("\"\"", "\"");
                row.Add(val);
            }

            if (row.Count >= 3)
            {
                string key = row[0].Trim();
                string ko = row[1].Trim();
                string en = row[2].Trim();

                if (!string.IsNullOrEmpty(key))
                {
                    _localizedText[key] = new Dictionary<ELanguage, string>
                    {
                        { ELanguage.KO, ko },
                        { ELanguage.EN, en }
                    };
                }
            }
        }
    }
}
