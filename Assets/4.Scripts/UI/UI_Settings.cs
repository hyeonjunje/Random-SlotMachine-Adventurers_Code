using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum ESettingGroup
{
    General,
    Graphics,
    Audios,
}

public class UI_Settings : UI_Base
{
    [SerializeField] private GameObject _general;
    [SerializeField] private GameObject _graphics;
    [SerializeField] private GameObject _audios;

    [Header("--- General Settings ---")]
    [SerializeField] private TMP_Dropdown _languageDropdown;
    [SerializeField] private Toggle _screenShakeToggle;
    [SerializeField] private Toggle _showTimerToggle;
    [SerializeField] private Toggle _skipIntroVideoToggle;
    [SerializeField] private Button _resetGeneralButton;

    [Header("--- Graphics Settings ---")]
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _vsyncToggle;
    [SerializeField] private TMP_Dropdown _fpsLimitDropdown;
    [SerializeField] private TMP_Dropdown _msaaDropdown;
    [SerializeField] private Button _resetGraphicsButton;

    [Header("--- Sound Settings ---")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private TMP_Text _masterVolumeText;
    
    [SerializeField] private Slider _bgmVolumeSlider;
    [SerializeField] private TMP_Text _bgmVolumeText;
    
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private TMP_Text _sfxVolumeText;
    
    [SerializeField] private Slider _ambientVolumeSlider;
    [SerializeField] private TMP_Text _ambientVolumeText;
    
    [SerializeField] private Toggle _muteInBackgroundToggle;
    [SerializeField] private Button _resetSoundButton;

    // --- Resolutions Cache ---
    private List<Resolution> _resolutions;

    public override void Initialize()
    {
        base.Initialize();
        
        BindUIEvents();
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        RefreshUIWithCurrentSettings();

        OnClickGroupTab((int)ESettingGroup.General);
    }

    public override void Close()
    {
        // 닫을 때 설정값 최종 저장 (디스크 기록)
        SettingsManager.Instance.SaveAllSettings();
        gameObject.SetActive(false);
    }

    public void OnClickCloseButton()
    {
        UIManager.Instance.Close(EUIType.UI_Settings);
    }

    /// <summary>
    /// UI 이벤트를 SettingsManager에 연결. null 체크를 통해 미할당된 UI 컴포넌트가 있어도 에러 방지
    /// </summary>
    private void BindUIEvents()
    {
        // General
        if (_languageDropdown != null) _languageDropdown.onValueChanged.AddListener(val => SettingsManager.Instance.SetLanguage((ELanguage)val));
        if (_screenShakeToggle != null) _screenShakeToggle.onValueChanged.AddListener(val => SettingsManager.Instance.SetScreenShake(val));
        if (_showTimerToggle != null) _showTimerToggle.onValueChanged.AddListener(val => SettingsManager.Instance.SetShowTimer(val));
        if (_skipIntroVideoToggle != null) _skipIntroVideoToggle.onValueChanged.AddListener(val => SettingsManager.Instance.SetSkipIntroVideo(val));
        if (_resetGeneralButton != null) _resetGeneralButton.onClick.AddListener(() => { SettingsManager.Instance.ResetGeneralSettings(); RefreshGeneralUI(); });

        // Graphics
        if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(val => {
            SettingsManager.Instance.SetFullscreen(val);
            UpdateResolutionInteractable();
            RefreshResolutionDropdownOptions(); // 전체 화면 모드에 따라 N/A 또는 해상도 목록 표시
        });
        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(val => SettingsManager.Instance.SetResolution(val));
        
        if (_vsyncToggle != null) _vsyncToggle.onValueChanged.AddListener(val => {
            SettingsManager.Instance.SetVSync(val);
            UpdateFpsLimitInteractable(); // VSync 설정에 따라 FPS 드롭다운 활성화/비활성화
            RefreshFpsLimitDropdownOptions(); // VSync 켜져있을 경우 N/A 표시
        });
        
        if (_fpsLimitDropdown != null) _fpsLimitDropdown.onValueChanged.AddListener(val => SettingsManager.Instance.SetFpsLimit(val));
        if (_msaaDropdown != null) _msaaDropdown.onValueChanged.AddListener(val => SettingsManager.Instance.SetMsaaLevel(val));
        if (_resetGraphicsButton != null) _resetGraphicsButton.onClick.AddListener(() => { SettingsManager.Instance.ResetGraphicsSettings(); RefreshGraphicsUI(); });

        // Sound
        if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(val => { SettingsManager.Instance.SetMasterVolume(val); UpdateVolumeText(_masterVolumeText, val); });
        if (_bgmVolumeSlider != null) _bgmVolumeSlider.onValueChanged.AddListener(val => { SettingsManager.Instance.SetBgmVolume(val); UpdateVolumeText(_bgmVolumeText, val); });
        if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.AddListener(val => { SettingsManager.Instance.SetSfxVolume(val); UpdateVolumeText(_sfxVolumeText, val); });
        if (_ambientVolumeSlider != null) _ambientVolumeSlider.onValueChanged.AddListener(val => { SettingsManager.Instance.SetAmbientVolume(val); UpdateVolumeText(_ambientVolumeText, val); });
        if (_muteInBackgroundToggle != null) _muteInBackgroundToggle.onValueChanged.AddListener(val => SettingsManager.Instance.SetMuteInBackground(val));
        if (_resetSoundButton != null) _resetSoundButton.onClick.AddListener(() => { SettingsManager.Instance.ResetSoundSettings(); RefreshSoundUI(); });
    }

    private void RefreshUIWithCurrentSettings()
    {
        RefreshGeneralUI();
        RefreshGraphicsUI();
        RefreshSoundUI();
    }

    private void RefreshGeneralUI()
    {
        var mgr = SettingsManager.Instance;
        if (_languageDropdown != null) _languageDropdown.SetValueWithoutNotify((int)mgr.Language);
        if (_screenShakeToggle != null) _screenShakeToggle.SetIsOnWithoutNotify(mgr.ScreenShake);
        if (_showTimerToggle != null) _showTimerToggle.SetIsOnWithoutNotify(mgr.ShowTimer);
        if (_skipIntroVideoToggle != null) _skipIntroVideoToggle.SetIsOnWithoutNotify(mgr.SkipIntroVideo);

        // 언어 설정은 타이틀 화면이 켜져있을 때만
        _languageDropdown.interactable = UIManager.Instance.Get<UI_Title>(EUIType.UI_Title).gameObject.activeSelf;
    }

    private void RefreshGraphicsUI()
    {
        var mgr = SettingsManager.Instance;
        
        RefreshResolutionDropdownOptions(); // 해상도 리스트 갱신
        RefreshFpsLimitDropdownOptions(); // FPS 리스트 갱신 (N/A 처리)

        if (_fullscreenToggle != null) _fullscreenToggle.SetIsOnWithoutNotify(mgr.Fullscreen);
        if (_resolutionDropdown != null) _resolutionDropdown.SetValueWithoutNotify(mgr.ResolutionIndex);
        if (_vsyncToggle != null) _vsyncToggle.SetIsOnWithoutNotify(mgr.VSync);
        if (_msaaDropdown != null) _msaaDropdown.SetValueWithoutNotify(mgr.MsaaLevelIndex);

        UpdateResolutionInteractable();
        UpdateFpsLimitInteractable();
    }

    private void UpdateResolutionInteractable()
    {
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.interactable = !SettingsManager.Instance.Fullscreen;
        }
    }

    private void UpdateFpsLimitInteractable()
    {
        if (_fpsLimitDropdown != null)
        {
            _fpsLimitDropdown.interactable = !SettingsManager.Instance.VSync;
        }
    }

    private void RefreshFpsLimitDropdownOptions()
    {
        if (_fpsLimitDropdown == null) return;

        var mgr = SettingsManager.Instance;
        _fpsLimitDropdown.ClearOptions();
        List<string> options = new List<string>();

        if (mgr.VSync)
        {
            options.Add("N/A");
            _fpsLimitDropdown.AddOptions(options);
            _fpsLimitDropdown.SetValueWithoutNotify(0); // 인덱스 0으로 고정
        }
        else
        {
            options.Add(LocalizationManager.Instance.Get("CS_UI_SETTINGS_020"));
            options.Add("30");
            options.Add("60");
            options.Add("120");
            options.Add("144");
            _fpsLimitDropdown.AddOptions(options);
            _fpsLimitDropdown.SetValueWithoutNotify(mgr.FpsLimitIndex);
        }
    }

    private void RefreshSoundUI()
    {
        var mgr = SettingsManager.Instance;
        if (_masterVolumeSlider != null) { _masterVolumeSlider.SetValueWithoutNotify(mgr.MasterVolume); UpdateVolumeText(_masterVolumeText, mgr.MasterVolume); }
        if (_bgmVolumeSlider != null) { _bgmVolumeSlider.SetValueWithoutNotify(mgr.BgmVolume); UpdateVolumeText(_bgmVolumeText, mgr.BgmVolume); }
        if (_sfxVolumeSlider != null) { _sfxVolumeSlider.SetValueWithoutNotify(mgr.SfxVolume); UpdateVolumeText(_sfxVolumeText, mgr.SfxVolume); }
        if (_ambientVolumeSlider != null) { _ambientVolumeSlider.SetValueWithoutNotify(mgr.AmbientVolume); UpdateVolumeText(_ambientVolumeText, mgr.AmbientVolume); }
        if (_muteInBackgroundToggle != null) _muteInBackgroundToggle.SetIsOnWithoutNotify(mgr.MuteInBackground);
    }

    private void UpdateVolumeText(TMP_Text textComponent, float value)
    {
        if (textComponent != null)
        {
            textComponent.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void RefreshResolutionDropdownOptions()
    {
        if (_resolutionDropdown == null) return;

        var mgr = SettingsManager.Instance;
        _resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        if (mgr.Fullscreen) // 전체 화면일 경우
        {
            options.Add("N/A");
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(0); // 인덱스 0으로 임의 고정
        }
        else
        {
            var availableRes = mgr.GetAvailableResolutions();
            foreach (var res in availableRes)
            {
                options.Add($"{res.width} x {res.height}");
            }
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(mgr.ResolutionIndex);
        }
    }

    #region UIEvent
    public void OnClickGroupTab(int index)
    {
        ESettingGroup selectedGroup = (ESettingGroup)index;

        _general.SetActive(selectedGroup == ESettingGroup.General);
        _graphics.SetActive(selectedGroup == ESettingGroup.Graphics);
        _audios.SetActive(selectedGroup == ESettingGroup.Audios);
    }
    #endregion
}

