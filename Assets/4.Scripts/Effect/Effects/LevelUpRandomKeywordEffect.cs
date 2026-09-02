using System.Collections.Generic;
using System.Linq;

// 랜덤으로 내가 보유한 키워드의 레벨을 올리는 Effect (ex 연타해라 -> 난타해라)
public class LevelUpRandomKeywordEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        List<SO_KeywordData> keywordDatas = new List<SO_KeywordData>();

        foreach (EKeyword adverbKeyword in DataManager.Instance.GameModel.AdverbKeywords)
        {
            SO_KeywordData adverbKeywordData = DataManager.Instance.GetKeywordData(adverbKeyword);
            keywordDatas.Add(adverbKeywordData);
        }

        foreach (EKeyword verbKeyword in DataManager.Instance.GameModel.VerbKeywords)
        {
            SO_KeywordData verbKeywordData = DataManager.Instance.GetKeywordData(verbKeyword);
            keywordDatas.Add(verbKeywordData);
        }

        List<SO_KeywordData> filteredKeywordDatas = keywordDatas.Where(keywordData => keywordData.UpgradedId != 0).ToList();

        if (filteredKeywordDatas.Count > 0)
        {
            return new LevelUpKeywordGA(filteredKeywordDatas[0].Keyword);
        }

        return new LevelUpKeywordGA(EKeyword.None);
    }
}
