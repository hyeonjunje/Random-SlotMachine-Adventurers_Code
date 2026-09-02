using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SO_AudioData", menuName = "Scriptable Objects/Manager/SO_AudioData")]
public class SO_AudioData : ScriptableObject
{
    [Serializable]
    public struct BgmEntry
    {
        public EBgmId id;
        public AudioClip clip;
        public float defaultVolume;
    }

    [Serializable]
    public struct SfxEntry
    {
        public ESfxId id;
        public AudioClip clip;
        public float defaultVolume;
    }

    [Header("Mixer")]
    [field: SerializeField] public AudioMixer Mixer { get; private set; }
    [field: SerializeField] public AudioMixerGroup MasterGroup { get; private set; }
    [field: SerializeField] public AudioMixerGroup BgmGroup { get; private set; }
    [field: SerializeField] public AudioMixerGroup SfxGroup { get; private set; }
    [field: SerializeField] public AudioMixerGroup AmbientGroup { get; private set; }
    [field: SerializeField] public string ExposedMasterParam { get; private set; } = "Master_Volume";
    [field: SerializeField] public string ExposedBgmParam { get; private set; } = "BGM_Volume";
    [field: SerializeField] public string ExposedSfxParam { get; private set; } = "SFX_Volume";
    [field: SerializeField] public string ExposedAmbientParam { get; private set; } = "Ambient_Volume";

    [Header("Tables")]
    [field:SerializeField] public SO_AudioCatalog_BGM BGMCatalog { get; private set; }
    [field: SerializeField] public SO_AudioCatalog_SFX SfxCatalog { get; private set; }

    private Dictionary<EBgmId, BgmEntry> _bgmMap;
    private Dictionary<ESfxId, SfxEntry> _sfxMap;

    public void BuildCache()
    {
        _bgmMap = new();
        _sfxMap = new();

        foreach(BgmEntry entry in BGMCatalog.Bgms)
        {
            if(entry.clip)
            {
                _bgmMap[entry.id] = entry;
            }
        }

        foreach (SfxEntry entry in SfxCatalog.Sfxs)
        {
            if (entry.clip)
            {
                _sfxMap[entry.id] = entry;
            }
        }
    }

    public bool TryGet(EBgmId id, out BgmEntry entry) => _bgmMap.TryGetValue(id, out entry);
    public bool TryGet(ESfxId id, out SfxEntry entry) => _sfxMap.TryGetValue(id, out entry);

#if UNITY_EDITOR
    private void OnValidate() => BuildCache();
#endif
}

