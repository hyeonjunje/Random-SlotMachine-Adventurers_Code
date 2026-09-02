using System.Collections.Generic;
using UnityEngine;

public class RemoveSlotMachineKeywordEffect : Effect
{
    [Header ("특정 키워드 제거일 경우")]
    [SerializeField] private EKeyword _keyword = EKeyword.None;

    [Header ("랜덤 제거일 경우")]
    [SerializeField] private bool _isRandom = false;
    [SerializeField] private EKeywordType _targetType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        EKeyword targetKeyword = _keyword;

        if (_isRandom)
        {
            targetKeyword = Utils.GetRandomOwnedKeyword (_targetType);
        }

        if (targetKeyword == EKeyword.None)
        {
            return null;
        }

        SO_KeywordData data = DataManager.Instance.GetKeywordData (targetKeyword);
        if (data != null && data.KeywordType == EKeywordType.Subject)
        {
            return null;
        }

        return new RemoveSlotMachineKeywordGA (targetKeyword);
    }
}