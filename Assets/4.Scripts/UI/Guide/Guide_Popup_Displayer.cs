using UnityEngine;
using UnityEngine.EventSystems;

public class Guide_Popup_Displayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private EHelpKey _helpKey;
    [SerializeField] private bool _isWorldSpace = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.ShowGuidePopup(_helpKey, transform, _isWorldSpace);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.HideGuidePopup(transform);
    }
}
