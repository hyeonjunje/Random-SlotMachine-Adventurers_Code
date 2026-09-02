using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_MyKeywords : UI_Base
{
    [Header("주어 키워드")]
    [SerializeField] private Transform _subjectKeywordParent;
    [SerializeField] private ListItem_HUDKeyword _subjectKeywordPrefab;

    [Header("부사 키워드")]
    [SerializeField] private Transform _adverbKeywordParent;
    [SerializeField] private ListItem_HUDKeyword _adverbKeywordPrefab;

    [Header("동사 키워드")]
    [SerializeField] private Transform _verbKeywordParent;
    [SerializeField] private ListItem_HUDKeyword _verbKeywordPrefab;

    [Header("저주 키워드")]
    [SerializeField] private Transform _curseKeywordParent;
    [SerializeField] private ListItem_HUDKeyword _curseKeywordPrefab;

    private List<ListItem_HUDKeyword> _subjectKeywords = new List<ListItem_HUDKeyword>();
    private List<ListItem_HUDKeyword> _adverbKeywords = new List<ListItem_HUDKeyword>();
    private List<ListItem_HUDKeyword> _verbKeywords = new List<ListItem_HUDKeyword>();
    private List<ListItem_HUDKeyword> _curseKeywords = new List<ListItem_HUDKeyword>();

    [SerializeField] private TMP_Text _guideText; 
    private Action<EKeyword> _onKeywordSelected;
    public bool IsSelectMode { get; private set; } = false;
    private bool _isUpgradeMode = false;
    public override void Initialize()
    {
        base.Initialize();

        _subjectKeywords.Add(_subjectKeywordPrefab);
        _adverbKeywords.Add(_adverbKeywordPrefab);
        _verbKeywords.Add(_verbKeywordPrefab);
        _curseKeywords.Add(_curseKeywordPrefab);
    }

    public override void Close()
    {
        gameObject.SetActive(false);
        _guideText.gameObject.SetActive(false);
    }

    public override void Open()
    {
        _onKeywordSelected = null;
        IsSelectMode = false;

        gameObject.SetActive (true);
        UpdateUI ();
    }

    public void OpenForSelect(Action<EKeyword> onSelected, bool isUpgradeMode = false)
    {
        _onKeywordSelected = onSelected;
        IsSelectMode = true;
        _isUpgradeMode = isUpgradeMode;

        gameObject.SetActive (true);

        if (_guideText != null)
        {
            _guideText.text = _isUpgradeMode ? "강화할 단어를 선택하세요." : "제거할 단어를 선택하세요.";
            _guideText.gameObject.SetActive (true);
        }

        UpdateUI ();
    }

    private void UpdateUI()
    {
        Init (); 

        var model = DataManager.Instance.GameModel;

        // 주어 키워드
        BindKeywords (model.SubjectKeywords, model.TempSubjectKeywords,
            _subjectKeywords, _subjectKeywordPrefab, _subjectKeywordParent, false);

        // 부사 키워드
        BindKeywords (model.AdverbKeywords, model.TempAdverbKeywords,
            _adverbKeywords, _adverbKeywordPrefab, _adverbKeywordParent);

        // 동사 키워드
        BindKeywords (model.VerbKeywords, model.TempVerbKeywords,
            _verbKeywords, _verbKeywordPrefab, _verbKeywordParent);

        // 저주 키워드
        BindKeywords (model.CurseKeywords, model.TempCurseKeywords,
            _curseKeywords, _curseKeywordPrefab, _curseKeywordParent);
    }

    private void BindKeywords(List<EKeyword> mainList, List<EKeyword> tempList,
        List<ListItem_HUDKeyword> uiList, ListItem_HUDKeyword prefab, Transform parent, bool canShowPreview = true)
    {
        List<EKeyword> allKeywords = new List<EKeyword> (mainList);
        allKeywords.AddRange (tempList);

        for (int i = 0; i < allKeywords.Count; ++i)
        {
            // 아이템 모자라면 새로 생성
            if (i >= uiList.Count)
            {
                var item = Instantiate (prefab, parent);
                uiList.Add (item);
            }

            var uiItem = uiList[i];
            uiItem.gameObject.SetActive (true);

            SO_KeywordData data = DataManager.Instance.GetKeywordData (allKeywords[i]);

            // 키워드 잠금 및 클릭 설정
            bool isLocked = false;
            Action<EKeyword, ListItem_HUDKeyword> onClickAction = OnItemClicked;

            if (IsSelectMode)
            {
                if (data.IsLocked || (data.KeywordType & EKeywordType.Subject) != 0 || (data.KeywordType & EKeywordType.Curse) != 0)
                {
                    isLocked = true;
                    onClickAction = null;
                }
                else if (_isUpgradeMode && !Utils.CanUpgrade (data))
                {
                    isLocked = true;
                    onClickAction = null;
                }
                else
                {
                    isLocked = false;
                    onClickAction = OnItemClicked;
                }
            }
            else
            {
                isLocked = false;
                onClickAction = null;
            }

            uiItem.SetListItem (data, onClickAction, isLocked, canShowPreview);
        }
    }
    private void Init()
    {
        foreach (ListItem_HUDKeyword keyword in _subjectKeywords)
        {
            keyword.gameObject.SetActive(false);
        }

        foreach (ListItem_HUDKeyword keyword in _adverbKeywords)
        {
            keyword.gameObject.SetActive(false);
        }

        foreach (ListItem_HUDKeyword keyword in _verbKeywords)
        {
            keyword.gameObject.SetActive(false);
        }

        foreach (ListItem_HUDKeyword keyword in _curseKeywords)
        {
            keyword.gameObject.SetActive(false);
        }
    }

    private void OnItemClicked(EKeyword keyword, ListItem_HUDKeyword item)
    {
        if (!IsSelectMode) return;
        IsSelectMode = false;

        if (_isUpgradeMode)
        {
            _onKeywordSelected?.Invoke (keyword);
            Close ();
        }
        else
        {
            item.PlayDeleteAnimation (() =>
            {
                _onKeywordSelected?.Invoke (keyword);
                Close ();
            });
        }
    }
}