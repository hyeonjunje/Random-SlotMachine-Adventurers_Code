using System;
using System.Collections;
using UnityEngine;

public class UI_SelectionContext : UI_Base
{
    [SerializeField] private GameObject _objBackground;
    [SerializeField] private Transform _pivotControl;

    private Transform _controlledTransform;
    private Transform _originParent;
    private Coroutine _coroutine;

    private SelectionResolver _selectionResolver;

    public override void Close()
    {
        _objBackground.SetActive(false);
    }

    public override void Open()
    {
        _objBackground.SetActive(false);
    }

    public void ResolveSelection(SelectionResolver selectionResolver, Action action)
    {
        _selectionResolver = selectionResolver;

        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(CoResolveSelection(selectionResolver, action));
    }

    private IEnumerator CoResolveSelection(SelectionResolver selectionResolver, Action action)
    {
        _objBackground.SetActive(true);

        _controlledTransform = FindObject(selectionResolver.TransformName);
        _originParent = _controlledTransform.parent;
        _controlledTransform.SetParent(_pivotControl.transform, false);

        yield return selectionResolver.CoResolveSelection();

        action?.Invoke();

        HideUI();
    }

    public void AddIndex(int index)
    {
        _selectionResolver.AddIndex(index);
    }

    public bool IsControlled(Transform tr)
    {
        while(tr.parent != null)
        {
            if(tr.parent == transform)
            {
                return true;
            }
            tr = tr.parent;
        }
        return false;
    }

    private Transform FindObject(string objectName)
    {
        Transform result = null;
        var allObjects = FindObjectsByType<Transform>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);

        foreach (var obj in allObjects)
        {
            if (obj.name == objectName)
            {
                result = obj;
                break;
            }
        }

        return result;
    }

    #region UIEvent
    public void HideUI()
    {
        _selectionResolver = null;

        _objBackground.SetActive(false);

        if (_controlledTransform != null)
        {
            _controlledTransform.SetParent(_originParent, false);
        }

        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
    }
    #endregion
}
