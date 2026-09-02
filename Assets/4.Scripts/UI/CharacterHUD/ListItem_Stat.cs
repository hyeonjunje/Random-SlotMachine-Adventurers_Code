using TMPro;
using UnityEngine;

public class ListItem_Stat : BaseListItem<Stat>
{
    [SerializeField] private TMP_Text _textStatName;
    [SerializeField] private TMP_Text _textStatValue;

    public override void SetListItem(Stat item)
    {
        base.SetListItem(item);

        _textStatName.text = Item.StatName;
        _textStatValue.text = Item.Value.ToString();
    }
}
