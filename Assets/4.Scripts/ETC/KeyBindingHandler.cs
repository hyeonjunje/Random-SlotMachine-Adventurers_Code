using System.Collections;
using UnityEngine;

public interface IReceiveKeyService
{
    public void Init();

    public void Update();
}

public class KeyBindingHandler : MonoBehaviour, IInitializable
{
    private IReceiveKeyService _receiveKeyHandler;

    public void Initialize()
    {
        switch (AppConfig.BootStrapperType)
        {
            case EBootstrapperType.Live:
            case EBootstrapperType.Demo:
                _receiveKeyHandler = new LiveReceiveKeyKervice();
                break;
            case EBootstrapperType.Debug:
                _receiveKeyHandler = new DebugReceiveKeyKervice();
                break;
            case EBootstrapperType.Custom1:
                _receiveKeyHandler = new DebugReceiveKeyKervice();
                break;
            case EBootstrapperType.Custom2:
                _receiveKeyHandler = new DebugReceiveKeyKervice();
                break;
        }

        _receiveKeyHandler.Init();
    }

    private void Update()
    {
        _receiveKeyHandler?.Update();
    }
}

public class LiveReceiveKeyKervice : IReceiveKeyService
{
    private UI_Pause _uiPause;

    public void Init()
    {
        _uiPause = UIManager.Instance.Get<UI_Pause>(EUIType.UI_Pause);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(_uiPause == null)
            {
                return;
            }

            if (_uiPause.gameObject.activeSelf)
            {
                UIManager.Instance.Close(EUIType.UI_Pause);
            }
            else
            {
                UIManager.Instance.Open(EUIType.UI_Pause);
            }
        }

        if (AppConfig.IsCheatEnabled &&
            Input.GetKeyDown(KeyCode.R) &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            ResetProgressForDebug();
        }
    }

    private void ResetProgressForDebug()
    {
        RunSaveService.DeleteSave();
        PlayerPrefs.DeleteKey(TutorialController.TutorialClearedKey);
        PlayerPrefs.Save();

        UI_Title titleUI = UIManager.Instance?.Get<UI_Title>(EUIType.UI_Title);
        titleUI?.RefreshContinueButton();

        Debug.Log("<color=cyan>[치트] 런 세이브와 튜토리얼 클리어 플래그를 초기화했습니다. Ctrl+Shift+R</color>");
    }
}

public class DebugReceiveKeyKervice : IReceiveKeyService
{
    private KeywordTester _keywordTester;

    public void Init()
    {
        _keywordTester = GameObject.FindAnyObjectByType<KeywordTester>(FindObjectsInactive.Include);
    }


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(_keywordTester != null)
            {
                _keywordTester.Toggle();
            }
        }

        // 시간 느리게
        if (AppConfig.IsCheatEnabled && Input.GetKeyDown(KeyCode.Z))
        {
            Time.timeScale = Mathf.Max(Time.timeScale - 0.1f, 0);
        }

        // 시간 원위치
        if (AppConfig.IsCheatEnabled && Input.GetKeyDown(KeyCode.X))
        {
            Time.timeScale = 1;
        }

        // 시간 빠르게
        if (AppConfig.IsCheatEnabled && Input.GetKeyDown(KeyCode.C))
        {
            Time.timeScale = Mathf.Max(Time.timeScale + 0.1f, 0);
        }

        if (AppConfig.IsCheatEnabled &&
            Input.GetKeyDown(KeyCode.R) &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            ResetProgressForDebug();
        }
    }

    private void ResetProgressForDebug()
    {
        RunSaveService.DeleteSave();
        PlayerPrefs.DeleteKey(TutorialController.TutorialClearedKey);
        PlayerPrefs.Save();

        UI_Title titleUI = UIManager.Instance?.Get<UI_Title>(EUIType.UI_Title);
        titleUI?.RefreshContinueButton();

        Debug.Log("<color=cyan>[치트] 런 세이브와 튜토리얼 클리어 플래그를 초기화했습니다. Ctrl+Shift+R</color>");
    }
}


