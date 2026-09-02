using UnityEngine;
using UnityEngine.UI;

public class UI_Tutorial : UI_Base
{
    [SerializeField] private TutorialController _tutorialController;
    [SerializeField] private Button _skipButton;

    public bool IsTutorialRunning => _tutorialController != null && _tutorialController.IsRunning;
    public bool CanStartSlotMachine => _tutorialController == null || _tutorialController.CanStartSlotMachine;
    public bool ShouldBlockSlotMachineStart => _tutorialController != null && _tutorialController.ShouldBlockSlotMachineStart;
    public bool ShouldHandleSlotConfirm => _tutorialController != null && _tutorialController.ShouldHandleSlotConfirm;
    public bool ShouldBlockSlotConfirm => _tutorialController != null && _tutorialController.ShouldBlockSlotConfirm;

    public override void Initialize()
    {
        base.Initialize();
        ResolveReferences();
        _tutorialController?.Initialize(this);
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        ResolveReferences();
        ShowSkipButton();
        UIManager.Instance.Close(EUIType.UI_MainHud);
        _tutorialController?.BeginTutorial();
    }

    public override void Close()
    {
        HideSkipButton();
        gameObject.SetActive(false);
    }

    public void OnClickDialogue()
    {
        _tutorialController?.OnClickDialogue();
    }

    public void OnTutorialBattleCleared()
    {
        _tutorialController?.OnTutorialBattleCleared();
    }

    public void OnBattleTokensCreated()
    {
        _tutorialController?.OnBattleTokensCreated();
    }

    public bool TryHandleSlotConfirm(System.Action onReadyForTargetSelect = null)
    {
        return _tutorialController == null || _tutorialController.TryHandleSlotConfirm(onReadyForTargetSelect);
    }

    public void OnSlotConfirmButtonClicked()
    {
        TryHandleSlotConfirm();
    }

    public void OnClickSkip()
    {
        _tutorialController?.SkipTutorial();
    }

    private void ResolveReferences()
    {
        if (_tutorialController == null)
        {
            _tutorialController = GetComponent<TutorialController>();
        }

        if (_tutorialController == null)
        {
            Debug.LogWarning($"{nameof(UI_Tutorial)} needs a {nameof(TutorialController)} reference.", this);
        }
    }

    private void ShowSkipButton()
    {
        if (_skipButton == null)
        {
            Debug.LogWarning($"{nameof(UI_Tutorial)} needs a skip button reference.", this);
            return;
        }

        _skipButton.gameObject.SetActive(true);
        _skipButton.onClick.RemoveAllListeners();
        _skipButton.onClick.AddListener(OnClickSkip);
    }

    private void HideSkipButton()
    {
        if (_skipButton == null)
        {
            return;
        }

        _skipButton.onClick.RemoveAllListeners();
        _skipButton.gameObject.SetActive(false);
    }
}
