using Cysharp.Threading.Tasks.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum EKeywordCardPreviewType
{
    Display, // 그냥 전시용  (도움말 팝업 안보여줌)
    StoreDisplay, // 상점 전시용  (도움말 팝업 그냥 안보여줌, 호버 시 UI_Store에 KeywordPreview나옴)
    Reward, // 보상용   (호버 시 도움말 팝업 나옴, 예. 보상 UI)
    Guide,  // 가이드용 (그냥 도움말 팝업 나옴, dP. 상점 전시 키워드 호버 시 옆에 나오는 카드)
}

public class KeywordCardPreviewUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _imageCardFrame;
    [SerializeField] private TMP_Text _textCardName;
    [SerializeField] private TMP_Text _textCardCost;
    [SerializeField] private TMP_Text _textCardDescription;

    [SerializeField] private GameObject _objCardIconCenter;
    [SerializeField] private Image _imageCardIconCenter;

    private SO_KeywordData _keywordData;
    private EKeywordCardPreviewType _keywordCardPreviewType = EKeywordCardPreviewType.Display;

    /// <summary>
    /// 상점, 보상용 미리보기 카드 보여주는 메소드
    /// </summary>
    /// <param name="keywordData">보여줄 키워드의 데이터</param>
    /// <param name="isHoverAction">true면 호버 시 키워드 가이드 팝업나오고 false면 그냥 디폴트로 나온다.</param>
    public void ShowCardView(SO_KeywordData keywordData, EKeywordCardPreviewType keywordCardPreviewType)
    {
        gameObject.SetActive(true);
        _keywordData = keywordData;
        _keywordCardPreviewType = keywordCardPreviewType;

        _objCardIconCenter.gameObject.SetActive(true);

        _imageCardIconCenter.sprite = SpriteManager.Instance.GetSprite(keywordData.KeywordSpriteName);

        _imageCardFrame.sprite = SpriteManager.Instance.GetSprite("Card_" + (ECardRank)keywordData.Rank);

        _textCardName.text = LocalizationManager.Instance.Get(keywordData.KeywordName);
        _textCardCost.text = string.Empty;

        Character owner = null;
        string rawDescription = LocalizationManager.Instance.Get(keywordData.KeywordExplain);

        // 파싱 및 팝업 정보 추출
        var keywords = TextParser.ParseBrackets(rawDescription, owner, out string finalDescription);

        _textCardDescription.text = finalDescription;

        if(_keywordCardPreviewType == EKeywordCardPreviewType.Guide)
        {
            // 기존 팝업 제거 및 새 팝업 표시
            foreach (var keyword in keywords)
            {
                UIManager.Instance.ShowGuidePopup(keyword.name, keyword.explanation, transform, true);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_keywordCardPreviewType == EKeywordCardPreviewType.Reward)
        {
            Character owner = null;
            string rawDescription = LocalizationManager.Instance.Get(_keywordData.KeywordExplain);

            // 파싱 및 팝업 정보 추출
            var keywords = TextParser.ParseBrackets(rawDescription, owner, out string finalDescription);

            // 기존 팝업 제거 및 새 팝업 표시
            foreach (var keyword in keywords)
            {
                UIManager.Instance.ShowGuidePopup(keyword.name, keyword.explanation, transform, false);
            }
        }
        else if(_keywordCardPreviewType == EKeywordCardPreviewType.StoreDisplay)
        {
            UI_Store uiStore = UIManager.Instance.Get<UI_Store>(EUIType.UI_Store);
            if (uiStore != null && uiStore.gameObject.activeInHierarchy)
            {
                uiStore.ShowCardPreview(_keywordData);
            }
            else
            {
                UIManager.Instance.ShowKeywordCardPreview(_keywordData, transform);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(_keywordCardPreviewType == EKeywordCardPreviewType.Reward)
        {
            UIManager.Instance.HideGuidePopup(transform, true);
        }
        else if (_keywordCardPreviewType == EKeywordCardPreviewType.StoreDisplay)
        {
            UI_Store uiStore = UIManager.Instance.Get<UI_Store>(EUIType.UI_Store);
            if (uiStore != null && uiStore.gameObject.activeInHierarchy)
            {
                uiStore.HideCardPreview();
            }
            else
            {
                UIManager.Instance.HideKeywordCardPreview();
            }
        }
    }

    public void HideCardView()
    {
        UIManager.Instance.HideGuidePopup(transform, false);
        gameObject.SetActive(false);
    }
}
