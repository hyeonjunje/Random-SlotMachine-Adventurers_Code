using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineSlot : MonoBehaviour
{
    private enum ESlotContentType
    {
        Text,
        Sprite,
    }

    [SerializeField] private TMP_Text _textSlot;
    [field: SerializeField] public Transform Contents;

    private RectTransform _rect;
    public RectTransform Rect 
    { 
        get
        {
            if(_rect == null)
            {
                _rect = GetComponent<RectTransform>();
            }

            return _rect;
        }
    }

    private Image _imageSlot;

    public void SetText(EKeyword keyword)
    {
        SetVisual(ESlotContentType.Text);

        SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(keyword);

        _textSlot.text = LocalizationManager.Instance.Get(keywordData.KeywordName);
        _textSlot.color = Utils.GetKeywordColor(keywordData.KeywordType);
    }

    public void SetText(string text)
    {
        SetVisual(ESlotContentType.Text);

        _textSlot.text = text;
    }

    // 미니게임용 스프라이트 세팅
    public void SetSprite(Sprite sprite)
    {
        SetVisual(ESlotContentType.Sprite);

        if (_imageSlot != null)
        {
            _imageSlot.sprite = sprite;
            _imageSlot.gameObject.SetActive(true);
        }
    }

    private void SetVisual(ESlotContentType slotContentType)
    {
        if(_textSlot != null)
        {
            _textSlot.gameObject.SetActive(slotContentType == ESlotContentType.Text);
        }

        if (_imageSlot == null)
        {
            _imageSlot = Contents.GetComponentInChildren<Image>();
        }

        if (_imageSlot != null)
        {
            _imageSlot.gameObject.SetActive(slotContentType == ESlotContentType.Sprite);
        }
    }
}
