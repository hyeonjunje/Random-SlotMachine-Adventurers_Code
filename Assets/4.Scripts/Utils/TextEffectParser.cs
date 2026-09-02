using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

// 효과의 종류 정의
public enum TextEffectType
{
    None,
    Shake, // 글자 흔들림
    Wave,  // 글자 웨이브
}

// 파싱된 효과 데이터
public class TextEffectData
{
    public TextEffectType Type;
    public int StartIndex;
    public int EndIndex;

    public TextEffectData(TextEffectType type, int start, int end)
    {
        Type = type;
        StartIndex = start;
        EndIndex = end;
    }
}

// 파싱 결과물
public struct ParseResult
{
    public string RenderableText; // <shake>는 지워지고 <color>는 남은 텍스트
    public List<TextEffectData> Effects;
}

public static class TextEffectParser
{
    // 커스텀 태그 정의
    private static readonly string SHAKE_TAG = "shake";
    private static readonly string WAVE_TAG = "wave";

    private static readonly Regex TAG_REGEX = new Regex(@"<(/?)(\w+)(?:=[^>]+)?>");

    public static ParseResult Parse(string rawText)
    {
        StringBuilder finalBuilder = new StringBuilder();
        List<TextEffectData> effects = new List<TextEffectData>();

        Stack<(TextEffectType type, int startIndex)> openTags = new Stack<(TextEffectType, int)>();

        int currentVisibleIndex = 0; // 태그를 제외한 실제 보여질 글자의 인덱스

        MatchCollection matches = TAG_REGEX.Matches(rawText);
        int lastMatchEnd = 0;

        foreach (Match match in matches)
        {
            // 1. 태그 앞의 일반 텍스트 처리
            string content = rawText.Substring(lastMatchEnd, match.Index - lastMatchEnd);
            finalBuilder.Append(content);
            currentVisibleIndex += content.Length;

            // 2. 태그 분석
            string tagName = match.Groups[2].Value.ToLower();
            bool isCloseTag = match.Groups[1].Value == "/";

            TextEffectType currentTagType = TextEffectType.None;
            if (tagName == SHAKE_TAG)
            {
                currentTagType = TextEffectType.Shake;
            }
            else if (tagName == WAVE_TAG)
            {
                currentTagType = TextEffectType.Wave;
            }

            if (currentTagType != TextEffectType.None)
            {
                if (!isCloseTag)
                {
                    openTags.Push((currentTagType, currentVisibleIndex));
                }
                else
                {
                    // 스택 상단 태그와 현재 닫는 태그 종류가 같을 때만 처리
                    if (openTags.Count > 0 && openTags.Peek().type == currentTagType)
                    {
                        var startInfo = openTags.Pop();
                        effects.Add(new TextEffectData(currentTagType, startInfo.startIndex, currentVisibleIndex - 1));
                    }
                }
            }
            else
            {
                // 커스텀 태그가 아니면(<color> 등) 빌더에 그대로 추가
                finalBuilder.Append(match.Value);
            }

            lastMatchEnd = match.Index + match.Length;
        }

        // 남은 뒷부분 텍스트 추가
        if (lastMatchEnd < rawText.Length)
        {
            finalBuilder.Append(rawText.Substring(lastMatchEnd));
        }

        return new ParseResult
        {
            RenderableText = finalBuilder.ToString(),
            Effects = effects
        };
    }
}
