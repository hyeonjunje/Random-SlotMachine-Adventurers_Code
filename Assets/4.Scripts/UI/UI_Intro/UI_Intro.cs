using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UI_Intro : UI_Base
{
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage _videoScreenUI;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.8f;

    [SerializeField] private VideoClip _videoClipKor;
    [SerializeField] private VideoClip _videoClipEng;

    private RenderTexture _dynamicRT;

    private void Start()
    {
        // 라이브버전, 데모버전일 때만 인트로 영상을 틀어준다.
        if (AppConfig.BootStrapperType == EBootstrapperType.Live || AppConfig.BootStrapperType == EBootstrapperType.Demo)
        {
            if(SettingsManager.Instance.SkipIntroVideo)
            {
                OnClickSkip();
            }
            else
            {
                _dynamicRT = new RenderTexture(1920, 1080, 24);
                _videoPlayer.targetTexture = _dynamicRT;
                _videoScreenUI.texture = _dynamicRT;

                _canvasGroup.alpha = 0f;

                if(PlayerPrefs.GetInt("Set_Language") == 0)
                {
                    _videoPlayer.clip = _videoClipKor;
                }
                else
                {
                    _videoPlayer.clip = _videoClipEng;
                }

                _videoPlayer.playOnAwake = false;
                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.Prepare();
            }
        }
        else
        {
            OnClickSkip();
        }
    }

    public override void Close()
    {
        gameObject.SetActive(false);

        if(_dynamicRT != null)
        {
            _dynamicRT.Release();
            Destroy(_dynamicRT);
        }
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        _canvasGroup.DOFade(1, _fadeDuration);
        _videoPlayer.Play();
        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        Open();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        _videoPlayer.loopPointReached -= OnVideoFinished;

        Close();

        UIManager.Instance.Open(EUIType.UI_Title);
    }

    #region UIEvent
    public void OnClickSkip()
    {
        _videoPlayer.Stop();
        OnVideoFinished(_videoPlayer);
    }
    #endregion
}
