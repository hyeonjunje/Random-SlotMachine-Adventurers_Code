using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListItem_Ability : BaseListItem<Ability>
{
    [SerializeField] private Image _imageAbility;

    public override void SetListItem(Ability item)
    {
        base.SetListItem(item);
        gameObject.SetActive(true);
        _imageAbility.sprite = item.AbilitySprite;
    }
}
