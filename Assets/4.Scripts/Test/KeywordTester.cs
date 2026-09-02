using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeywordTester : MonoBehaviour
{
    [SerializeField] private Transform _myKeywords;
    [SerializeField] private Transform _attackVerbKeywords;
    [SerializeField] private Transform _defenseVerbKeywords;
    [SerializeField] private Transform _adverbKeywords;

    [SerializeField] private GameObject _myKeywordPrefab;
    [SerializeField] private Button _verbKeywordButtonPrefab;
    [SerializeField] private Button _adverbKeywordButtonPrefab;
    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void Start()
    {
        UpdateMyKeyword();
        UpdateKeywordPool();
    }

    private void UpdateMyKeyword()
    {
        _myKeywords.DestroyAllChildren();

        foreach(EKeyword keyword in DataManager.Instance.GameModel.VerbKeywords)
        {
            GameObject obj = Instantiate(_myKeywordPrefab, _myKeywords);
            obj.GetComponentInChildren<TMP_Text>().text = keyword.ToString();
            obj.gameObject.SetActive(true);
        }

        foreach (EKeyword keyword in DataManager.Instance.GameModel.AdverbKeywords)
        {
            GameObject obj = Instantiate(_myKeywordPrefab, _myKeywords);
            obj.GetComponentInChildren<TMP_Text>().text = keyword.ToString();
            obj.gameObject.SetActive(true);
        }
    }

    private void UpdateKeywordPool()
    {
        foreach(SO_KeywordData keywordData in DataManager.Instance.AllVerbKeywords)
        {
            if(keywordData.Keyword >= EKeyword.방어해라)  // 방어 동사
            {
                Button button = Instantiate(_verbKeywordButtonPrefab, _defenseVerbKeywords);
                button.GetComponentInChildren<TMP_Text>().text = keywordData.Keyword.ToString();
                button.onClick.AddListener(() => OnClickVerbKeyword(keywordData.Keyword));
                button.gameObject.SetActive(true);
            }
            else // 공격 동사
            {
                Button button = Instantiate(_verbKeywordButtonPrefab, _attackVerbKeywords);
                button.GetComponentInChildren<TMP_Text>().text = keywordData.Keyword.ToString();
                button.onClick.AddListener(() => OnClickVerbKeyword(keywordData.Keyword));
                button.gameObject.SetActive(true);
            }
        }

        // 부사
        foreach (SO_KeywordData keywordData in DataManager.Instance.AllAdverbKeywords)
        {
            Button button = Instantiate(_adverbKeywordButtonPrefab, _adverbKeywords);
            button.GetComponentInChildren<TMP_Text>().text = keywordData.Keyword.ToString();
            button.onClick.AddListener(() => OnClickAdVerbKeyword(keywordData.Keyword));
            button.gameObject.SetActive(true);
        }
    }

    private void OnClickVerbKeyword(EKeyword keyword)
    {
        DataManager.Instance.GameModel.VerbKeywords.Add(keyword);
        UpdateMyKeyword();
    }

    private void OnClickAdVerbKeyword(EKeyword keyword)
    {
        DataManager.Instance.GameModel.AdverbKeywords.Add(keyword);
        UpdateMyKeyword();
    }

    #region UIEvent
    public void OnClickReset()
    {
        DataManager.Instance.GameModel.AdverbKeywords.Clear();
        DataManager.Instance.GameModel.VerbKeywords.Clear();
        UpdateMyKeyword();
    }
    #endregion
}
