using TMPro;
using UnityEngine;

public class Guide_CharacterDetails : Guide_Base<Player>
{
    [SerializeField] private Transform _pivotSkeletonGraphic;
    [SerializeField] private GameObject _objLevelText;
    [SerializeField] private TMP_Text _textLevel;
    [SerializeField] private TMP_Text _textSkillDescription;
    [SerializeField] private ListItem_Stat _listItemStatAttackPower;
    [SerializeField] private ListItem_Stat _listItemMaxHp;

    public override void ShowGuide(Player player)
    {
        _pivotSkeletonGraphic.DestroyAllChildren();
        Instantiate(player.PlayerData.CharacterSkeletonGraphic, _pivotSkeletonGraphic, false);
        _textLevel.text = player.Level.ToString();

        if (player.BattleSideType == EBattleSideType.OurSide)
        {
            _objLevelText.SetActive(true);
            _textLevel.text = $"{player.Level.ToString()}";
        }
        else
        {
            _objLevelText.SetActive(false);
        }

        _listItemStatAttackPower.SetListItem(player.GetStat(EStatType.AttackPower));
        _listItemMaxHp.SetListItem (player.GetStat (EStatType.MaxHp));
    }
}
