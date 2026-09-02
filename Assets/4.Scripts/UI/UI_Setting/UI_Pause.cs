using UnityEngine;

public class UI_Pause : UI_Base
{
    public override void Close()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        UIManager.Instance.IsLock = false;
    }

    public override void Open()
    {
        Time.timeScale = 0f;
        gameObject.SetActive(true);
        UIManager.Instance.HideAllGuidePopup(false);
        UIManager.Instance.IsLock = true;
    }

    #region UIEvent
    public void OnClickSetting()
    {
        UIManager.Instance.Open(EUIType.UI_Settings);
    }

    public void OnClickContinue()
    {
        UIManager.Instance.Close(EUIType.UI_Pause);
    }

    public void OnClickRestart()
    {
        SceneManagerEx.Instance.LoadScene(0);
    }
    #endregion
}
