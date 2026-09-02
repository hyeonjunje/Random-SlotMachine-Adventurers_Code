using System;
using System.Collections.Generic;

// 내가 소유한 부사, 동사 키워드 중 무작위 하나 제거하는 Effect
public class RemoveRandomSlotMachineKeywordEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        List<EKeyword> keywords = new List<EKeyword>();
        keywords.AddRange(DataManager.Instance.GameModel.AdverbKeywords);
        keywords.AddRange(DataManager.Instance.GameModel.VerbKeywords);
        EKeyword randomSelectedKeyword = keywords[UnityEngine.Random.Range(0, keywords.Count)];

        return new RemoveSlotMachineKeywordGA(randomSelectedKeyword);
    }
}
