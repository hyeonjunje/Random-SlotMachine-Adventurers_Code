using System.Collections.Generic;
using UnityEngine;

public class StatusView : MonoBehaviour
{
    [SerializeField] private ListItem_Status _listItemStatusOrigin;
    [SerializeField] private ListItem_Ability _listItemAbilityOrigin;

    private List<ListItem_Status> _pools = new List<ListItem_Status>();
    private Dictionary<EStatusType, ListItem_Status> _statuses = new Dictionary<EStatusType, ListItem_Status>();

    private List<ListItem_Ability> _poolsAbility = new List<ListItem_Ability>();
    private StatusController _statusController;

    public void Init(StatusController statusController)
    {
        _statusController = statusController;

        _statusController.OnAddAbility += AddAbility;
        _statusController.OnAddStatus += AddStatus;
        _statusController.OnReleaseStatus += ReleaseStatus;
        _statusController.OnUpdateStatus += RefreshStatus;
    }

    public void Release()
    {
        _statusController.OnAddAbility -= AddAbility;
        _statusController.OnAddStatus -= AddStatus;
        _statusController.OnReleaseStatus -= ReleaseStatus;
        _statusController.OnUpdateStatus -= RefreshStatus;
    }

    private void AddAbility(Ability ability)
    {
        ListItem_Ability listItem = GetListItemAbility();
        listItem.SetListItem(ability);
    }
    
    private void AddStatus(Status status)
    {
        if(_statuses.TryGetValue(status.StatusType, out ListItem_Status listItem))
        {
            listItem.Refresh();
        }
        else
        {
            listItem = GetListItemStatus();
            _statuses.Add(status.StatusType, listItem);
            listItem.SetListItem(status);
        }
    }

    private void ReleaseStatus(EStatusType statusType)
    {
        if(_statuses.TryGetValue(statusType, out ListItem_Status listItem))
        {
            listItem.Release();
            _statuses.Remove(statusType);
        }
    }

    private void RefreshStatus(Status statusType)
    {
        if (_statuses.TryGetValue(statusType.StatusType, out ListItem_Status listItem))
        {
            listItem.Refresh();
        }
    }

    private ListItem_Ability GetListItemAbility()
    {
        foreach(ListItem_Ability listItem in _poolsAbility)
        {
            if(listItem.gameObject.activeSelf == false)
            {
                return listItem;
            }
        }

        ListItem_Ability newListItem = Instantiate(_listItemAbilityOrigin, transform);
        _poolsAbility.Add(newListItem);
        return newListItem;
    }

    private ListItem_Status GetListItemStatus()
    {
        foreach (ListItem_Status listItem in _pools)
        {
            if (listItem.gameObject.activeSelf == false)
            {
                return listItem;
            }
        }

        ListItem_Status newListItem = Instantiate(_listItemStatusOrigin, transform);
        _pools.Add(newListItem);
        return newListItem;
    }
}
