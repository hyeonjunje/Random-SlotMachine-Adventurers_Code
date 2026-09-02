using DG.Tweening;
using TMPro;
using UnityEngine;

public enum EEndingType
{
    Defeat,
    Victory,
}

public enum EScoreType
{
    Time,
    Island,
    Gold,
    Artifact,
    Keyword,
}

public class UI_Ending : UI_Base
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _objVictory;
    [SerializeField] private GameObject _objDefeat;
    [SerializeField] private GameObject _objGoBack;

    [SerializeField] private GameObject _objScoreTime;
    [SerializeField] private GameObject _objScoreIsland;
    [SerializeField] private GameObject _objScoreGold;
    [SerializeField] private GameObject _objScoreArtifact;
    [SerializeField] private GameObject _objScoreKeyword;

    [SerializeField] private TMP_Text _textScoreTime;
    [SerializeField] private TMP_Text _textScoreIsland;
    [SerializeField] private TMP_Text _textScoreGold;
    [SerializeField] private TMP_Text _textScoreArtifact;
    [SerializeField] private TMP_Text _textScoreKeyword;

    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _scoreInterval = 0.2f;

    private EEndingType _endingType;

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        _objVictory.SetActive(false);
        _objDefeat.SetActive(false);
        _objGoBack.SetActive(false);

        _objScoreTime.SetActive(false);
        _objScoreIsland.SetActive(false);
        _objScoreGold.SetActive(false);
        _objScoreArtifact.SetActive(false);
        _objScoreKeyword.SetActive(false);

        _canvasGroup.alpha = 0;

        AudioManager.Instance.StopBGM();

        _canvasGroup.DOFade(1, _fadeDuration)
            .OnComplete(() => ShowUI());
    }

    public void SetEndindType(EEndingType endingType)
    {
        _endingType = endingType;
    }

    private void ShowUI()
    {
        _objVictory.SetActive(_endingType == EEndingType.Victory);
        _objDefeat.SetActive(_endingType == EEndingType.Defeat);

        Sequence seq = DOTween.Sequence();

        seq.AppendCallback(() => ShowScore(EScoreType.Time))
            .AppendInterval(_scoreInterval)
            .AppendCallback(() => ShowScore(EScoreType.Island))
            .AppendInterval(_scoreInterval)
            .AppendCallback(() => ShowScore(EScoreType.Gold))
            .AppendInterval(_scoreInterval)
            .AppendCallback(() => ShowScore(EScoreType.Artifact))
            .AppendInterval(_scoreInterval)
            .AppendCallback(() => ShowScore(EScoreType.Keyword))
            .AppendInterval(_scoreInterval)
            .AppendCallback(() => _objGoBack.SetActive(true));
    }

    private void ShowScore(EScoreType scoreType)
    {
        switch (scoreType)
        {
            case EScoreType.Time:
                float currentElapsed = DataManager.Instance != null ? DataManager.Instance.GameModel.ElapsedTime : 0f;
                int minutes = Mathf.FloorToInt(currentElapsed / 60f);
                int seconds = Mathf.FloorToInt(currentElapsed % 60f);

                _objScoreTime.SetActive(true);
                _textScoreTime.text = string.Format(LocalizationManager.Instance.Get("UI_UI_ENDING_04"), $"{minutes:D2}:{seconds:D2}");
                break;
            case EScoreType.Island:
                _objScoreIsland.SetActive(true);
                _textScoreIsland.text = string.Format(LocalizationManager.Instance.Get("UI_UI_ENDING_05"), DataManager.Instance.GameModel.EnteredIslandCount);
                break;
            case EScoreType.Gold:
                _objScoreGold.SetActive(true);
                _textScoreGold.text = string.Format(LocalizationManager.Instance.Get("UI_UI_ENDING_06"), DataManager.Instance.GameModel.GainedGold);
                break;
            case EScoreType.Artifact:
                _objScoreArtifact.SetActive(true);
                _textScoreArtifact.text = string.Format(LocalizationManager.Instance.Get("UI_UI_ENDING_07"), DataManager.Instance.GameModel.GainedArtifact);
                break;
            case EScoreType.Keyword:
                _objScoreKeyword.SetActive(true);
                _textScoreKeyword.text = string.Format(LocalizationManager.Instance.Get("UI_UI_ENDING_08"), DataManager.Instance.GameModel.GainedKeyword);
                break;
        }
    }

    #region UIEvent
    public void OnClickGoBack()
    {
        // 그냥 처음으로 돌아감
        SceneManagerEx.Instance.LoadScene(0);
    }
    #endregion
}
