using UnityEngine;

public class UI_Title : UI_Base
{
    [SerializeField] private GameObject _objButtonStart;
    [SerializeField] private GameObject _objButtonContinue;
    [SerializeField] private GameObject titleObj;
    
    private GameObject _titleObjInstance;
    
    public override void Initialize()
    {
        base.Initialize ();
    }

    public override void Open()
    {
        UIManager.Instance.Close(EUIType.UI_MainHud);
        
        if (titleObj != null && _titleObjInstance == null)
        {
            _titleObjInstance = Instantiate(titleObj);
        }
        
        gameObject.SetActive (true);
        _objButtonStart.SetActive (true);

        AudioManager.Instance.PlayBGM(EBgmId.Title);
        RefreshContinueButton();
    }

    public void RefreshContinueButton()
    {
        _objButtonContinue.SetActive (RunSaveService.HasSave ());
    }

    public override void Close()
    {
        if (_titleObjInstance != null)
        {
            Destroy(_titleObjInstance);
            _titleObjInstance = null;
        }
        
        gameObject.SetActive (false);
    }

    public void OnClickStartStage()
    {
        RunSaveService.DeleteSave ();

        FindAnyObjectByType<ActionSystem>()?.CancelAllActions();
        FindAnyObjectByType<ArtifactSystem>()?.ClearAllArtifacts();
        FindAnyObjectByType<UIManager>()?.ClearArtifactPopupQueue();

        _objButtonStart.SetActive (false);
        _objButtonContinue.SetActive (false);

        if (PlayerPrefs.GetInt(TutorialController.TutorialClearedKey, 0) == 0 && UIManager.Instance.HasUI(EUIType.UI_Tutorial))
        {
            UIManager.Instance.Open(EUIType.UI_Tutorial);
            Close();
            return;
        }

        UIManager.Instance.Open(EUIType.UI_SelectCharacter);
        Close();
    }
    public void OnClickExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void OnClickContinueStage()
    {
        if (RunSaveLoader.TryContinueLatest ())
        {
            Close ();
        }
    }
    public void OnClickSettings()
    {
        UIManager.Instance.Open(EUIType.UI_Settings);
    }
}
