using System.Collections.Generic;
using UnityEngine;

public class AddSlotMachineKeywordEffect : Effect
{
    [Header("특정 키워드일 경우")]
    private EKeyword _keyword = EKeyword.None;

    [Header("랜덤일 경우")]
    [SerializeField] private bool _isRandom = false;
    [SerializeField] private EKeywordType _slotMachineKeywordTypeFlag;

    [SerializeField, Header("0은 확률계산안함"), Range(0,1)] private float _probability = 0;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        EKeyword keyword = _keyword;

        if (_isRandom)
        {
            SO_KeywordData selectedKeywordData = Utils.GetRandomKeywordData(_slotMachineKeywordTypeFlag);
            keyword = selectedKeywordData.Keyword;
        }

        // 확률값이 0이거나 확률안에 들어온다면 주어진 키워드를 추가한다. 
        if (_probability == 0 || _probability != 0 && Random.Range(0, 1) < _probability)
        {
            return new AddSlotMachineKeywordGA(keyword);
        }
        return new AddSlotMachineKeywordGA(EKeyword.None);
    }
}
