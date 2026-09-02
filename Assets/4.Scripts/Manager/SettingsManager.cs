using System;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : SingletonScene<SettingsManager>
{
    [Header("Default Data")]
    [SerializeField] private SO_DefaultSettings _defaultSettings;

    // --- PlayerPrefs Keys ---
    private readonly string KEY_LANGUAGE = "Set_Language";
    private readonly string KEY_SCREEN_SHAKE = "Set_ScreenShake";
    private readonly string KEY_SHOW_TIMER = "Set_ShowTimer";
    private readonly string KEY_SKIP_INTRO = "Set_SkipIntro";
    
    private readonly string KEY_FULLSCREEN = "Set_Fullscreen";
    private readonly string KEY_RESOLUTION = "Set_Resolution";
    private readonly string KEY_VSYNC = "Set_VSync";
    private readonly string KEY_FPS_LIMIT = "Set_FpsLimit";
    private readonly string KEY_MSAA_LEVEL = "Set_MsaaLevel";
    
    private readonly string KEY_MASTER_VOLUME = "Set_MasterVolume";
    private readonly string KEY_BGM_VOLUME = "Set_BgmVolume";
    private readonly string KEY_SFX_VOLUME = "Set_SfxVolume";
    private readonly string KEY_AMBIENT_VOLUME = "Set_AmbientVolume";
    private readonly string KEY_MUTE_IN_BACKGROUND = "Set_MuteInBackground";

    // --- General Settings ---
    public ELanguage Language { get; private set; }
    public bool ScreenShake { get; private set; }
    public bool ShowTimer { get; private set; }
    public bool SkipIntroVideo { get; private set; }

    // --- Graphics Settings ---
    public bool Fullscreen { get; private set; }
    public int ResolutionIndex { get; private set; }
    public bool VSync { get; private set; }
    public int FpsLimitIndex { get; private set; }
    public int MsaaLevelIndex { get; private set; }

    // --- Cached Resolutions ---
    private List<Resolution> _availableResolutions = new List<Resolution>();

    // --- Sound Settings ---
    public float MasterVolume { get; private set; }
    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public float AmbientVolume { get; private set; }
    public bool MuteInBackground { get; private set; }

    // Events (UI나 매니저들이 구독해서 실제 기능 반영)
    public event Action OnGraphicsSettingsChanged;
    public event Action<bool> OnScreenShakeChanged;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBgmVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;
    public event Action<float> OnAmbientVolumeChanged;

    protected override void OnAwakeSingleton()
    {
        LoadAllSettings();
    }

    private void LoadAllSettings()
    {
        // General
        Language = (ELanguage)PlayerPrefs.GetInt(KEY_LANGUAGE, _defaultSettings ? (int)_defaultSettings.Language : 0);
        ScreenShake = PlayerPrefs.GetInt(KEY_SCREEN_SHAKE, (_defaultSettings && _defaultSettings.ScreenShake) ? 1 : 0) == 1;
        ShowTimer = PlayerPrefs.GetInt(KEY_SHOW_TIMER, (_defaultSettings && _defaultSettings.ShowTimer) ? 1 : 0) == 1;
        SkipIntroVideo = PlayerPrefs.GetInt(KEY_SKIP_INTRO, (_defaultSettings && _defaultSettings.SkipIntroVideo) ? 1 : 0) == 1;

        // Graphics
        Fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, (_defaultSettings && _defaultSettings.Fullscreen) ? 1 : 0) == 1;
        ResolutionIndex = PlayerPrefs.GetInt(KEY_RESOLUTION, _defaultSettings ? _defaultSettings.ResolutionIndex : -1);
        VSync = PlayerPrefs.GetInt(KEY_VSYNC, (_defaultSettings && _defaultSettings.VSync) ? 1 : 0) == 1;
        FpsLimitIndex = PlayerPrefs.GetInt(KEY_FPS_LIMIT, _defaultSettings ? _defaultSettings.FpsLimitIndex : 2); // 60 FPS
        MsaaLevelIndex = PlayerPrefs.GetInt(KEY_MSAA_LEVEL, _defaultSettings ? _defaultSettings.MsaaLevelIndex : 2); // 4x

        // Sound
        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, _defaultSettings ? _defaultSettings.MasterVolume : 1f);
        BgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, _defaultSettings ? _defaultSettings.BgmVolume : 1f);
        SfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, _defaultSettings ? _defaultSettings.SfxVolume : 1f);
        AmbientVolume = PlayerPrefs.GetFloat(KEY_AMBIENT_VOLUME, _defaultSettings ? _defaultSettings.AmbientVolume : 1f);
        MuteInBackground = PlayerPrefs.GetInt(KEY_MUTE_IN_BACKGROUND, (_defaultSettings && _defaultSettings.MuteInBackground) ? 1 : 0) == 1;

        // 최초 1회 저장값이 없을 경우 현재 해상도로 초기화
        if (ResolutionIndex == -1)
        {
            var resList = GetAvailableResolutions();
            ResolutionIndex = GetCurrentResolutionIndex(resList);
            PlayerPrefs.SetInt(KEY_RESOLUTION, ResolutionIndex);
        }

        ApplyGraphicsSettings();
    }

    public void SaveAllSettings()
    {
        PlayerPrefs.Save();
    }

    public void ResetGeneralSettings()
    {
        if (_defaultSettings == null) return;
        SetLanguage(_defaultSettings.Language);
        SetScreenShake(_defaultSettings.ScreenShake);
        SetShowTimer(_defaultSettings.ShowTimer);
        SetSkipIntroVideo(_defaultSettings.SkipIntroVideo);
    }

    public void ResetGraphicsSettings()
    {
        if (_defaultSettings == null) return;
        SetFullscreen(_defaultSettings.Fullscreen);
        SetResolution(_defaultSettings.ResolutionIndex);
        SetVSync(_defaultSettings.VSync);
        SetFpsLimit(_defaultSettings.FpsLimitIndex);
        SetMsaaLevel(_defaultSettings.MsaaLevelIndex);
    }

    public void ResetSoundSettings()
    {
        if (_defaultSettings == null) return;
        SetMasterVolume(_defaultSettings.MasterVolume);
        SetBgmVolume(_defaultSettings.BgmVolume);
        SetSfxVolume(_defaultSettings.SfxVolume);
        SetAmbientVolume(_defaultSettings.AmbientVolume);
        SetMuteInBackground(_defaultSettings.MuteInBackground);
    }

    // --- Setters (PlayerPrefs 저장 및 이벤트 호출) ---
    
    public void SetLanguage(ELanguage val) { Language = val; PlayerPrefs.SetInt(KEY_LANGUAGE, (int)val); if (LocalizationManager.Instance != null) LocalizationManager.Instance.ChangeLanguage(val); }
    public void SetScreenShake(bool val) { ScreenShake = val; PlayerPrefs.SetInt(KEY_SCREEN_SHAKE, val ? 1 : 0); OnScreenShakeChanged?.Invoke(val); }
    public void SetShowTimer(bool val) { ShowTimer = val; PlayerPrefs.SetInt(KEY_SHOW_TIMER, val ? 1 : 0); }
    public void SetSkipIntroVideo(bool val) { SkipIntroVideo = val; PlayerPrefs.SetInt(KEY_SKIP_INTRO, val ? 1 : 0); }

    public void SetFullscreen(bool val) { Fullscreen = val; PlayerPrefs.SetInt(KEY_FULLSCREEN, val ? 1 : 0); ApplyGraphicsSettings(); }
    public void SetResolution(int val) { ResolutionIndex = val; PlayerPrefs.SetInt(KEY_RESOLUTION, val); ApplyGraphicsSettings(); }
    public void SetVSync(bool val) { VSync = val; PlayerPrefs.SetInt(KEY_VSYNC, val ? 1 : 0); ApplyGraphicsSettings(); }
    public void SetFpsLimit(int val) { FpsLimitIndex = val; PlayerPrefs.SetInt(KEY_FPS_LIMIT, val); ApplyGraphicsSettings(); }
    public void SetMsaaLevel(int val) { MsaaLevelIndex = val; PlayerPrefs.SetInt(KEY_MSAA_LEVEL, val); ApplyGraphicsSettings(); }

    public void SetMasterVolume(float val) { MasterVolume = val; PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, val); OnMasterVolumeChanged?.Invoke(val); }
    public void SetBgmVolume(float val) { BgmVolume = val; PlayerPrefs.SetFloat(KEY_BGM_VOLUME, val); OnBgmVolumeChanged?.Invoke(val); }
    public void SetSfxVolume(float val) { SfxVolume = val; PlayerPrefs.SetFloat(KEY_SFX_VOLUME, val); OnSfxVolumeChanged?.Invoke(val); }
    public void SetAmbientVolume(float val) { AmbientVolume = val; PlayerPrefs.SetFloat(KEY_AMBIENT_VOLUME, val); OnAmbientVolumeChanged?.Invoke(val); }
    public void SetMuteInBackground(bool val) { MuteInBackground = val; PlayerPrefs.SetInt(KEY_MUTE_IN_BACKGROUND, val ? 1 : 0); }

    // --- Graphics Applier ---
    private void ApplyGraphicsSettings()
    {
        // 1. FPS Limit (0: Uncapped, 1: 30, 2: 60, 3: 120, 4: 144)
        int[] fpsOptions = { -1, 30, 60, 120, 144 };
        int targetFps = fpsOptions[Mathf.Clamp(FpsLimitIndex, 0, fpsOptions.Length - 1)];
        Application.targetFrameRate = targetFps;

        // 2. VSync
        QualitySettings.vSyncCount = VSync ? 1 : 0;

        // 3. MSAA (0: Off, 1: 2x, 2: 4x, 3: 8x)
        int[] msaaOptions = { 0, 2, 4, 8 };
        QualitySettings.antiAliasing = msaaOptions[Mathf.Clamp(MsaaLevelIndex, 0, msaaOptions.Length - 1)];

        // 4. Resolution & Window Mode
        FullScreenMode fsMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        
        var available = GetAvailableResolutions();
        if (available.Count > 0)
        {
            Resolution targetRes;
            
            // 전체화면일 때는 유저가 창모드 시절에 셋팅해둔 작은 해상도를 무시하고
            // 모니터가 지원하는 가장 큰 원본 해상도로 강제로 꽉 채움. 
            if (Fullscreen)
            {
                targetRes = available[available.Count - 1]; // 배열의 마지막이 모니터의 최대 해상도
            }
            else
            {
                if (ResolutionIndex >= available.Count) 
                {
                    ResolutionIndex = available.Count - 1;
                    PlayerPrefs.SetInt(KEY_RESOLUTION, ResolutionIndex);
                }
                if (ResolutionIndex < 0) ResolutionIndex = 0;

                targetRes = available[ResolutionIndex];
            }

            Screen.SetResolution(targetRes.width, targetRes.height, fsMode);
        }
        
        OnGraphicsSettingsChanged?.Invoke();
    }

    public List<Resolution> GetAvailableResolutions()
    {
        _availableResolutions.Clear();
        Resolution[] allResolutions = Screen.resolutions;

        Dictionary<string, Resolution> uniqueRes = new Dictionary<string, Resolution>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            var res = allResolutions[i];
            
            if (res.width < 1000) continue;

            string key = $"{res.width}x{res.height}";
            if (!uniqueRes.ContainsKey(key))
            {
                uniqueRes[key] = res;
            }
            else
            {
                // 동일 해상도면 가장 높은 주사율로 덮어쓰기
                if (res.refreshRateRatio.value > uniqueRes[key].refreshRateRatio.value)
                {
                    uniqueRes[key] = res;
                }
            }
        }

        foreach (var res in uniqueRes.Values)
        {
            _availableResolutions.Add(res);
        }

        if (_availableResolutions.Count == 0)
        {
            _availableResolutions.Add(Screen.currentResolution);
        }

        return _availableResolutions;
    }

    private int GetCurrentResolutionIndex(List<Resolution> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (Screen.width == list[i].width && Screen.height == list[i].height)
            {
                return i;
            }
        }
        return list.Count - 1;
    }
}
