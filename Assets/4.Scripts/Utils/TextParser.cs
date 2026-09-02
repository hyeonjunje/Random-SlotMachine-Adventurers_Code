using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class TextParser
{
    private static readonly Regex _keywordRegex = new Regex(@"\{(\w+)\}");
    private static readonly Regex _bracketRegex = new Regex(@"\[(.*?)\]");

    private static Dictionary<string, string> _parseMap = new();

    private static Dictionary<string, Func<Character, float>> _parseMapWithOwner = new();
    private static Dictionary<string, Func<Character, float>> _originMapWithOwner = new();

    private const string BUFFCOLOR = "#44FF44";
    private const string DEBUFFCOLOR = "#FF4444";

    private const EColorKey SQUAREBRACKETCOLOR = EColorKey.Orange;     // 대괄호 색깔
    private const EColorKey CURLYBRACKETCOLOR = EColorKey.DeBuffSkill; // 중괄호 색깔

    public static void Init()
    {
        SO_GameModel model = DataManager.Instance.GameModel;

        _parseMap = new Dictionary<string, string>
        {
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_021"), LocalizationManager.Instance.Get("CS_TEXTPARSER_022")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_023"), LocalizationManager.Instance.Get("CS_TEXTPARSER_024")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_025"), LocalizationManager.Instance.Get("CS_TEXTPARSER_026")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_027"), LocalizationManager.Instance.Get("CS_TEXTPARSER_028")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_029"), LocalizationManager.Instance.Get("CS_TEXTPARSER_030")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_031"), LocalizationManager.Instance.Get("CS_TEXTPARSER_032")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_033"), LocalizationManager.Instance.Get("CS_TEXTPARSER_034")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_035"), LocalizationManager.Instance.Get("CS_TEXTPARSER_036")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_037"), LocalizationManager.Instance.Get("CS_TEXTPARSER_038")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_039"), LocalizationManager.Instance.Get("CS_TEXTPARSER_040")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_041"), LocalizationManager.Instance.Get("CS_TEXTPARSER_042")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_043"), LocalizationManager.Instance.Get("CS_TEXTPARSER_044")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_045"), LocalizationManager.Instance.Get("CS_TEXTPARSER_046") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_047"), LocalizationManager.Instance.Get("CS_TEXTPARSER_048") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_049"), LocalizationManager.Instance.Get("CS_TEXTPARSER_050") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_051"), LocalizationManager.Instance.Get("CS_TEXTPARSER_052") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_053"), LocalizationManager.Instance.Get("CS_TEXTPARSER_054") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_055"), LocalizationManager.Instance.Get("CS_TEXTPARSER_056") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_057"), LocalizationManager.Instance.Get("CS_TEXTPARSER_058") },
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_059"), LocalizationManager.Instance.Get("CS_TEXTPARSER_060")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_061"), LocalizationManager.Instance.Get("CS_TEXTPARSER_062")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_063"), LocalizationManager.Instance.Get("CS_TEXTPARSER_064")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_065"), LocalizationManager.Instance.Get("CS_TEXTPARSER_066")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_067"), LocalizationManager.Instance.Get("CS_TEXTPARSER_068")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_069"), LocalizationManager.Instance.Get("CS_TEXTPARSER_070")},
            {LocalizationManager.Instance.Get("CS_TEXTPARSER_071"), LocalizationManager.Instance.Get("CS_TEXTPARSER_072")}
        };

        _parseMapWithOwner = new Dictionary<string, Func<Character, float>>
        {
            {"AttackPower", (owner) => owner != null ? owner.GetStat(EStatType.AttackPower).Value : 0 },  // 공격력
            {"WeakeningValue", (owner) => owner != null && owner is Player player ? model.WeakeningValue : GameDefine.WEAKENING_VALUE},                        // 약화
            {"MarkingValue", (owner) => owner != null && owner is Player player ? model.MarkingValue : GameDefine.MARKING_VALUE},                              // 표식
            {"EletricValue", (owner) => owner != null && owner is Player player ? model.EletricValue : GameDefine.ELETRIC_VALUE},                              // 감전
            {"CounterAttackValue", (owner) => owner != null && owner is Player player ? model.CounterAttackValue : GameDefine.COUNTERATTACK_VALUE},          // 반격
            {"PunishmentAttackValue", (owner) => owner != null && owner is Player player ? model.PunishmentAttackValue : GameDefine.PUNISHMENTATTACK_VALUE}, // 응징
            {"GuardianValue", (owner) => owner != null && owner is Player player ? model.GuardianValue : GameDefine.GUARDIAN_VALUE},                         // 수호
            {"PreservationValue", (owner) => owner != null && owner is Player player ? model.PreservationValue : GameDefine.PRESERVATION_VALUE},             // 보존
        };

        _originMapWithOwner = new Dictionary<string, Func<Character, float>>
        {
            {"AttackPower", (owner) => owner != null ? owner.GetStat(EStatType.AttackPower).BaseValue : 0 },  // 공격력
            {"WeakeningValue", (owner) => GameDefine.WEAKENING_VALUE},                    // 약화
            {"MarkingValue", (owner) => GameDefine.MARKING_VALUE},                        // 표식
            {"EletricValue", (owner) => GameDefine.ELETRIC_VALUE},                        // 감전
            {"CounterAttackValue", (owner) => GameDefine.COUNTERATTACK_VALUE},            // 반격
            {"PunishmentAttackValue", (owner) => GameDefine.PUNISHMENTATTACK_VALUE},      // 응징
            {"GuardianValue", (owner) => GameDefine.GUARDIAN_VALUE},                      // 수호
            {"PreservationValue", (owner) => GameDefine.PRESERVATION_VALUE},              // 보존
        };
    }

    #region Public API

    /// <summary>
    /// rawText 에서 대괄호([])와 중괄호({})를 파싱하여 컬러라이즈된 텍스트와 키워드 설명 목록을 반환.
    /// 대괄호는 SQUAREBRACKETCOLOR로 컬러라이즈되고 _parseMap을 통해 키워드 설명을 재귀적으로 수집.
    /// 중괄호는 _parseMapWithOwner를 통해 실제 수치로 치환.
    /// </summary>
    /// <param name="rawText">원본 텍스트</param>
    /// <param name="owner">해당 문구와 관련된 Character, null 해도 상관없음.</param>
    /// <param name="colorizedText">대괄호·중괄호를 처리한 결과 텍스트</param>
    /// <returns>
    /// rawText에서 대괄호로 된 키워드를 재귀적으로 찾아 _parseMap에 따라 파싱된 (제목, 내용) 리스트.
    /// 호출부에서 UIManager.Instance.ShowGuidePopup를 하기 위함.
    /// </returns>
    public static HashSet<(string name, string explanation)> ParseBrackets(string rawText, Character owner, out string colorizedText)
    {
        var keywords = new HashSet<(string, string)>();
        var visited = new HashSet<string>();
        string hexColor = ColorUtility.ToHtmlStringRGB(StyleManager.Instance.GetColor(SQUAREBRACKETCOLOR));

        colorizedText = _bracketRegex.Replace(rawText, (match) =>
        {
            string keyword = match.Groups[1].Value;
            if (visited.Add(keyword) && _parseMap.TryGetValue(keyword, out string explanation))
            {
                string parsedExplanation = Parse(explanation, owner);
                string colorizedExplanation = _bracketRegex.Replace(parsedExplanation, m => $"<color=#{hexColor}>{m.Groups[1].Value}</color>");
                keywords.Add((keyword, colorizedExplanation));
                CollectKeywordsRecursive(parsedExplanation, owner, keywords, visited, hexColor);
            }

            return $"<color=#{hexColor}>{keyword}</color>";
        });

        colorizedText = Parse(colorizedText, owner);

        return keywords;
    }

    /// <summary>
    /// RawText에서 중괄호에 들어간 값들을 파싱한 값을 반환
    /// </summary>
    /// <param name="rawText">파싱할 원본 텍스트</param>
    /// <param name="owner">해당 문구와 관련된 Owner, 캐릭터가 없으면 null 해도 상관없음</param>
    /// <returns></returns>
    public static string Parse(string rawText, Character owner)
    {
        if (string.IsNullOrEmpty(rawText)) return "";

        return _keywordRegex.Replace(rawText, (match) =>
        {
            // match.Groups[1].Value는 중괄호를 뺀 내부 단어 (예: "AttackPower")
            string keyword = match.Groups[1].Value;

            return GetValueByKeyword(keyword, owner);
        });
    }

    /// <summary>
    /// 문자열 내에 포함된 수식 (예: "<color=#F75F5F>4</color> X 150%") 을 찾아 계산 후 수식을 결과값으로 치환합니다.
    /// 계산 결과는 소수점 첫째 자리에서 반올림됩니다.
    /// </summary>
    /// <param name="text">수식이 포함된 텍스트</param>
    /// <returns>수식이 계산되어 치환된 텍스트</returns>
    public static string EvaluateMathExpressions(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // 정규식 매치 패턴: (접두 태그 옵션)숫자(접미 태그 옵션) 사칙연산기호 숫자(%옵션)
        // 지원되는 예시: "<color=#F75F5F>4</color> X 150%", "10 + 50%", "10 - 2"
        string pattern = @"(?<prefix><[^>]+>)?(?<num1>\d+(?:\.\d+)?)(?<suffix></[^>]+>)?\s*(?<op>[+\-*Xx/])\s*(?<num2>\d+(?:\.\d+)?)(?<percent>%)?";

        return Regex.Replace(text, pattern, match =>
        {
            float n1 = float.Parse(match.Groups["num1"].Value);
            float n2 = float.Parse(match.Groups["num2"].Value);
            bool isPercent = match.Groups["percent"].Success;
            string op = match.Groups["op"].Value.ToUpper();

            float n2Value = isPercent ? n2 / 100f : n2;
            float result = 0f;

            switch (op)
            {
                case "+":
                    result = n1 + (isPercent ? n1 * n2Value : n2Value);
                    break;
                case "-":
                    result = n1 - (isPercent ? n1 * n2Value : n2Value);
                    break;
                case "*":
                case "X":
                    result = n1 * n2Value;
                    break;
                case "/":
                    result = n2Value != 0 ? n1 / n2Value : 0;
                    break;
            }

            int roundedResult = Mathf.RoundToInt(result);

            string prefix = match.Groups["prefix"].Value;
            string suffix = match.Groups["suffix"].Value;

            return $"{prefix}{roundedResult}{suffix}";
        });
    }

    #endregion

    #region Inner Method
    private static void CollectKeywordsRecursive(string text, Character owner, HashSet<(string, string)> keywords, HashSet<string> visited, string hexColor)
    {
        foreach (Match match in _bracketRegex.Matches(text))
        {
            string keyword = match.Groups[1].Value;
            if (!visited.Add(keyword)) continue;

            if (_parseMap.TryGetValue(keyword, out string explanation))
            {
                string parsedExplanation = Parse(explanation, owner);
                string colorizedExplanation = _bracketRegex.Replace(parsedExplanation, m => $"<color=#{hexColor}>{m.Groups[1].Value}</color>");
                keywords.Add((keyword, colorizedExplanation));
                CollectKeywordsRecursive(parsedExplanation, owner, keywords, visited, hexColor);
            }
        }
    }


    /// <summary>
    /// _parseMapWithOwner 랑 _originMapWithOwner 의 키값을 입력받으면 비교해서 원본과 달라졌다는걸 보여주는 텍스트를 반환
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="owner"></param>
    /// <returns></returns>
    private static string GetValueByKeyword(string keyword, Character owner)
    {
        float value = 0, originValue = 0;

        if (_parseMapWithOwner.TryGetValue(keyword, out var handler))
        {
            value = handler(owner);
        }

        if (_originMapWithOwner.TryGetValue(keyword, out var originhandler))
        {
            originValue = originhandler(owner);
        }

        // 일단 임시로 퍼센트 표시로 이렇게 구현, 이것도 나중에 문서로 뺄 때 수정
        bool isPercent = keyword != "AttackPower";
        string formatted = FormatValue(value, isPercent);
        return formatted;

        // 빨간색으로 하니까 글자가 안보임 그래서 주석
        string hexColor = ColorUtility.ToHtmlStringRGB(StyleManager.Instance.GetColor(CURLYBRACKETCOLOR));
        return $"<color=#{hexColor}>{formatted}</color>";

        /*// 값이 원본과 달라졌다는걸 색을 입힘으로서 보여주자.
        if (value > originValue)
        {
            return $"<color={BUFFCOLOR}>{formatted}</color>";
        }
        else if (value < originValue)
        {
            return $"<color={DEBUFFCOLOR}>{formatted}</color>";
        }
        else
        {
            return formatted;
        }*/
    }

    private static string FormatValue(float value, bool isPercent)
    {
        if (isPercent)
        {
            return $"{value * 100f:0}%";
        }

        return value.ToString();
    }
    #endregion
}

