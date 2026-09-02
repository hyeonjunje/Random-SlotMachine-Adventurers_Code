using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string _localizationKey;
    
    private TMP_Text _textComponent;

    private void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText(LocalizationManager.Instance.CurrentLanguage);
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    /// <summary>
    /// 동적으로 로컬라이제이션 키를 변경해야 할 때 사용합니다.
    /// </summary>
    public void SetKey(string newKey)
    {
        _localizationKey = newKey;
        if (LocalizationManager.Instance != null)
        {
            UpdateText(LocalizationManager.Instance.CurrentLanguage);
        }
    }

    private void UpdateText(ELanguage language)
    {
        if (string.IsNullOrEmpty(_localizationKey))
            return;

        if (_textComponent != null)
        {
            _textComponent.text = LocalizationManager.Instance.Get(_localizationKey);
        }
    }
}
