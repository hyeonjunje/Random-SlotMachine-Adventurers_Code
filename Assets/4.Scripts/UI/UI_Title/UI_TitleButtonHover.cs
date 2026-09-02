using UnityEngine;
using UnityEngine.EventSystems;

public class UI_TitleButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject leftHover;
    [SerializeField] private GameObject rightHover;

    public void OnPointerEnter(PointerEventData eventData)
    {
        leftHover.SetActive (true);
        rightHover.SetActive (true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        leftHover.SetActive (false);
        rightHover.SetActive (false);
    }
}
