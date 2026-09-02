using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListItem_EventChoice : BaseListItem<ChoiceData>, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text _textChoice;
    [SerializeField] private GameObject _objLock;
    [SerializeField] private GameObject _objHighlight;

    public override void SetListItem(ChoiceData item)
    {
        base.SetListItem(item);
        gameObject.SetActive(true);
        _textChoice.text = LocalizationManager.Instance.Get(item.ChoiceExplain);

        if(item.Condition != null)
        {
            item.Condition.SetOwner(CharacterSystem.Instance.Players[0]);
        }

        // 조건 감지
        if(item.Condition != null && item.Condition.SubConditionIsMet(null) == false)
        {
            _objLock.SetActive(true);
        }
        else
        {
            _objLock.SetActive(false);
        }

        _objHighlight.SetActive(false);
    }

    public void Release()
    {
        gameObject.SetActive(false);
    }

    #region UIEvent
    public void OnClickChoice()
    {
        if(_objLock.activeSelf)
        {
            return;
        }

        PerformEventChoiceEffectGA performEventChoiceEffectGA = new PerformEventChoiceEffectGA(Item);
        ActionSystem.Instance.Perform(performEventChoiceEffectGA);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _objHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _objHighlight.SetActive(false);
    }
    #endregion
}
