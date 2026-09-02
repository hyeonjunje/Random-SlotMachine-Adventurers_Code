using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_PlayerSkill : BaseListItem<SO_SkillData>, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _imageSkillIcon;    
    [SerializeField] private TextMeshProUGUI _textSkillName; 
    [SerializeField] private Image _imageFrame;
    [SerializeField] private Image _imageFocusFrame;

    private Action<ListItem_PlayerSkill> _onClickCallback;

    public void InitializeForReward(SO_SkillData data, Action<ListItem_PlayerSkill> onClick)
    {
        SetListItem (data);
        _onClickCallback = onClick;
    }

    public override void SetListItem(SO_SkillData data)
    {
        base.SetListItem (data);

        // 데이터 반영
        _imageSkillIcon.sprite = SpriteManager.Instance.GetSprite(data.SkillIconName);

        _textSkillName.text = data.SkillName;

        _imageFrame.gameObject.SetActive (true);
        _imageFocusFrame.gameObject.SetActive (false);

        gameObject.SetActive (true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClickCallback?.Invoke (this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive (false);
        _imageFocusFrame.gameObject.SetActive (true);

        // 스킬 설명 툴팁 출력
        UIManager.Instance.ShowGuidePopup (Item.SkillDescription, Item.SkillName, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive (true);
        _imageFocusFrame.gameObject.SetActive (false);
        UIManager.Instance.HideGuidePopup (transform);
    }
}