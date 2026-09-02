using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusController
{
    private Dictionary<EStatusType, Status> _statuses = new Dictionary<EStatusType, Status>();
    public IReadOnlyDictionary<EStatusType, Status> Statuses => _statuses;

    private List<Status> _delayedStatus = new List<Status>();
    public IReadOnlyList<Status> DelayedStatus => _delayedStatus;

    public event Action<Ability> OnAddAbility;
    public event Action<Status> OnAddStatus;
    public event Action<EStatusType> OnReleaseStatus;
    public event Action<Status> OnUpdateStatus;

    public Ability Ability { get; private set; }

    public StatusController()
    {
        _statuses = new Dictionary<EStatusType, Status>();
    }

    public void Release()
    {
        foreach (Status status in _statuses.Values)
        {
            status.Release();
        }

        _statuses.Clear();

        OnAddAbility = null;
        OnAddStatus = null;
        OnReleaseStatus = null;
        OnUpdateStatus = null;
    }

    public bool IsStatus(EStatusType statusType)
    {
        return _statuses.ContainsKey(statusType);
    }

    public int GetStack(EStatusType statusType)
    {
        if(_statuses.TryGetValue(statusType, out Status status))
        {
            return status.RemainTurn;
        }
        else
        {
            return 0;
        }
    }

    public void AddAbility(Ability newAbility)
    {
        OnAddAbility?.Invoke(newAbility);
    }

    public void AddStatus(Status newStatus)
    {
        // 기존에 이미 있으면 늘려준다.
        if (_statuses.TryGetValue(newStatus.StatusType, out Status status))
        {
            status.AddTurn(newStatus.RemainTurn);
        }
        // 없으면 새로 넣어준다.
        else
        {
            _statuses.Add(newStatus.StatusType, newStatus);
            newStatus.Add();
        }

        OnAddStatus?.Invoke(_statuses[newStatus.StatusType]);
    }

    public void ReleaseStatus(EStatusType statusType)
    {
        // 있으면 없애주고 해제해준다.
        if (_statuses.TryGetValue(statusType, out Status status))
        {
            status.Release();
            _statuses.Remove(statusType);
        }

        OnReleaseStatus?.Invoke(statusType);
    }

    public void UpdateStatus(Status status)
    {
        // 해당 Status 갱신
        _statuses[status.StatusType] = status;
        OnUpdateStatus?.Invoke(status);
    }

    public List<Status> GetStatusesByCategory(EStatusCategory StatusCategory)
    {
        List<Status> result = new List<Status>();
        foreach(Status status in _statuses.Values)
        {
            if(status.StatusCategory == StatusCategory)
            {
                result.Add(status);
            }
        }
        return result;
    }

    public void ResetForLoad()
    {
        List<EStatusType> statusTypes = new List<EStatusType> (_statuses.Keys);

        foreach (EStatusType statusType in statusTypes)
        {
            if (_statuses.TryGetValue (statusType, out Status status))
            {
                status.Release ();
            }

            _statuses.Remove (statusType);
            OnReleaseStatus?.Invoke (statusType);
        }
    }

    public void AddDelayedStatus(Status status)
    {
        _delayedStatus.Add(status);
    }

    public void ClearDelayedStatus()
    {
        _delayedStatus.Clear();
    }
}
