using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCardPreviewUI : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.2f;

    [Header("Ally Side")]
    [SerializeField] private Vector2 _allyStartPos = new Vector2(300, -500);
    [SerializeField] private Vector2 _allyEndPos = new Vector2(300, 50);

    [Header("Enemy Side")]
    [SerializeField] private Vector2 _enemyStartPos = new Vector2(-300, -500);
    [SerializeField] private Vector2 _enemyEndPos = new Vector2(-300, 50);

    [Space(10)]
    [SerializeField] private Image _imageCardFrame;
    [SerializeField] private TMP_Text _textCardName;
    [SerializeField] private TMP_Text _textCardCost;
    [SerializeField] private TMP_Text _textCardDescription;

    [SerializeField] private GameObject _objCardIconLeft;
    [SerializeField] private GameObject _objCardIconRight;
    [SerializeField] private GameObject _objCardIconCenter;
    [SerializeField] private Image _imageCardIconLeft;
    [SerializeField] private Image _imageCardIconRight;
    [SerializeField] private Image _imageCardIconCenter;

    public void ShowCardView(BattleAct battleAct)
    {
        gameObject.SetActive(true);

        if (battleAct.Skill.SubjectKeyword == EKeyword.None)
        {
            SetCard_Simple(battleAct);
        }
        else
        {
            SetCard_Combination(battleAct);
        }

        SetPos(battleAct);
        SetCard_Common(battleAct);
    }

    public void HideCardView(Action onComplete = null)
    {
        UIManager.Instance.HideGuidePopup(transform, false);
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    // 어떤 행동이든 공통적인 정보를 담는 부분
    private void SetCard_Common(BattleAct battleAct)
    {
        _textCardName.text = battleAct.Skill.SkillName;

        _textCardCost.gameObject.SetActive(battleAct.Skill.ManaCost != 0);
        _textCardCost.text = battleAct.Skill.ManaCost.ToString();

        Character owner = battleAct.CharacterView?.Character;
        string rawDescription = battleAct.Skill.SkillDescription;

        // 파싱 및 팝업 정보 추출
        var keywords = TextParser.ParseBrackets(rawDescription, owner, out string finalDescription);
        finalDescription = TextParser.EvaluateMathExpressions(finalDescription);
        _textCardDescription.text = finalDescription;

        // 기존 팝업 제거 및 새 팝업 표시
        foreach (var keyword in keywords)
        {
            UIManager.Instance.ShowGuidePopup(keyword.name, keyword.explanation, transform, true);
        }
    }

    // BattleAct가 단순한 행동일 때 (예, 적 행동)
    private void SetCard_Simple(BattleAct battleAct)
    {
        _objCardIconLeft.gameObject.SetActive(false);
        _objCardIconRight.gameObject.SetActive(false);
        _objCardIconCenter.gameObject.SetActive(true);

        _imageCardIconCenter.sprite = SpriteManager.Instance.GetSprite(battleAct.Skill.CenterSkillIconName);

        _imageCardFrame.sprite = SpriteManager.Instance.GetSprite("Card_" + ECardRank.Rainbow);
    }

    // BattleAct가 부사와 동사의 조합일 때 (예, 연속으로 난타해라)
    private void SetCard_Combination(BattleAct battleAct)
    {
        _objCardIconLeft.gameObject.SetActive(true);
        _objCardIconRight.gameObject.SetActive(true);
        _objCardIconCenter.gameObject.SetActive(false);

        Skill skill = battleAct.Skill;

        _imageCardIconLeft.sprite = SpriteManager.Instance.GetSprite(skill.LeftSkillIconName);
        _imageCardIconRight.sprite = SpriteManager.Instance.GetSprite(skill.RightSkillIconName);

        int totalRank = skill.AdverbKeyword.KeywordData.Rank + skill.VerbKeyword.KeywordData.Rank;
        if(totalRank > (int)ECardRank.Rainbow)
        {
            totalRank = (int)ECardRank.Rainbow;
        }
        _imageCardFrame.sprite = SpriteManager.Instance.GetSprite("Card_" + (ECardRank)totalRank);
    }

    // 우리 편이냐 상태 편이냐 에 따라 위치 설정
    private void SetPos(BattleAct battleAct)
    {
        bool isAlly = true;
        if (battleAct.CharacterView != null)
        {
            isAlly = battleAct.CharacterView.Character.BattleSideType == EBattleSideType.OurSide;
        }

        Vector2 targetPos = isAlly ? _allyEndPos : _enemyEndPos;
        _rectTransform.anchoredPosition = targetPos;
    }
}
