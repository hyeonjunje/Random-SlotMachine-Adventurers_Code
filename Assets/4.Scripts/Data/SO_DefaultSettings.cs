using UnityEngine;

[CreateAssetMenu(fileName = "SO_DefaultSettings", menuName = "Scriptable Objects/SO_DefaultSettings")]
public class SO_DefaultSettings : ScriptableObject
{
    [field: Header("General")]
    [field: SerializeField] public ELanguage Language { get; private set; } = ELanguage.KO;
    [field: SerializeField] public bool ScreenShake { get; private set; } = true;
    [field: SerializeField] public bool ShowTimer { get; private set; } = true;
    [field: SerializeField] public bool SkipIntroVideo { get; private set; } = false;

    [field: Header("Graphics")]
    [field: SerializeField] public bool Fullscreen { get; private set; } = true;
    [field: SerializeField] public int ResolutionIndex { get; private set; } = 0;
    [field: SerializeField] public bool VSync { get; private set; } = true;
    [field: SerializeField] public int FpsLimitIndex { get; private set; } = 2; // 0: 무제한, 1: 30, 2: 60, 3: 120, 4: 144
    [field: SerializeField] public int MsaaLevelIndex { get; private set; } = 2; // 0: 끄기, 1: 2x, 2: 4x, 3: 8x

    [field: Header("Sound")]
    [field: SerializeField, Range(0f, 1f)] public float MasterVolume { get; private set; } = 1f;
    [field: SerializeField, Range(0f, 1f)] public float BgmVolume { get; private set; } = 1f;
    [field: SerializeField, Range(0f, 1f)] public float SfxVolume { get; private set; } = 1f;
    [field: SerializeField, Range(0f, 1f)] public float AmbientVolume { get; private set; } = 1f;
    [field: SerializeField] public bool MuteInBackground { get; private set; } = false;
}
