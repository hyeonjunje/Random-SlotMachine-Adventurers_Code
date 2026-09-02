using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : SingletonScene<AudioManager>
{
    [SerializeField] private SO_AudioData _catalog;

    [Header("AudioSources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxPrefab;
    [SerializeField] private int _sfxPoolSize = 8;

    private readonly Queue<AudioSource> _pool = new();
    private EBgmId _currentBgm = EBgmId.None;

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();

        _catalog.BuildCache();

        if (_catalog.BgmGroup && _bgmSource)
        {
            _bgmSource.outputAudioMixerGroup = _catalog.BgmGroup;
        }

        if (_catalog.SfxGroup && _sfxPrefab)
        {
            _sfxPrefab.outputAudioMixerGroup = _catalog.SfxGroup;
        }

        for (int i = 0; i < _sfxPoolSize; ++i)
        {
            AudioSource audioSource = Instantiate(_sfxPrefab, transform);
            audioSource.playOnAwake = false;
            audioSource.gameObject.SetActive(false);
            _pool.Enqueue(audioSource);
        }
    }

    private void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnMasterVolumeChanged += SetMasterVolume01;
            SettingsManager.Instance.OnBgmVolumeChanged += SetBgmVolume01;
            SettingsManager.Instance.OnSfxVolumeChanged += SetSfxVolume01;
            SettingsManager.Instance.OnAmbientVolumeChanged += SetAmbientVolume01;

            // 씬 시작 시 현재 저장된 볼륨값으로 세팅 동기화
            SetMasterVolume01(SettingsManager.Instance.MasterVolume);
            SetBgmVolume01(SettingsManager.Instance.BgmVolume);
            SetSfxVolume01(SettingsManager.Instance.SfxVolume);
            SetAmbientVolume01(SettingsManager.Instance.AmbientVolume);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnMasterVolumeChanged -= SetMasterVolume01;
            SettingsManager.Instance.OnBgmVolumeChanged -= SetBgmVolume01;
            SettingsManager.Instance.OnSfxVolumeChanged -= SetSfxVolume01;
            SettingsManager.Instance.OnAmbientVolumeChanged -= SetAmbientVolume01;
        }
    }

    public void PlayBGM(EBgmId id, float fade = 0.35f, bool loop = true)
    {
        if (_currentBgm == id)
        {
            return;
        }

        _currentBgm = id;

        if (!_catalog.TryGet(id, out var entry) || entry.clip == null)
        {
            StopBGM(fade);
            return;
        }

        // Play할 BGM과 기존 BGM이 같으면 return
        if(entry.clip == _bgmSource.clip)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeInBgm(entry.clip, fade, loop, Mathf.Clamp01(entry.defaultVolume <= 0 ? 1f : entry.defaultVolume)));
    }
    
    public void StopBGM(float fade = 0.25f)
    {
        StartCoroutine(FadeOut(_bgmSource, fade));
    }
    
    public void PlaySFX(ESfxId id, Vector3? position = null, float? volumeOverride = null)
    {
        if (!_catalog.TryGet(id, out var entry) || entry.clip == null)
        {
            return;
        }

        var src = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_sfxPrefab, transform);
        src.transform.position = position ?? Vector3.zero;
        src.clip = entry.clip;
        src.volume = Mathf.Clamp01(volumeOverride ?? (entry.defaultVolume <= 0 ? 1f : entry.defaultVolume));
        src.gameObject.SetActive(true);
        src.Play();
        StartCoroutine(RecycleWhenDone(src));
    }
    
    private float ToDecibel(float volume)
    {
        volume = Mathf.Clamp01(volume);
        // 볼륨이 0에 가까우면 -80dB(음소거)로 처리
        if (volume <= 0.0001f) return -80f;
        // 유니티 AudioMixer의 dB는 로그 스케일이므로 변환
        return Mathf.Log10(volume) * 20f;
    }

    public void SetMasterVolume01(float volume)
    {
        if (_catalog != null && _catalog.Mixer != null)
            _catalog.Mixer.SetFloat(_catalog.ExposedMasterParam, ToDecibel(volume));
    }

    public void SetBgmVolume01(float volume)
    {
        if (_catalog != null && _catalog.Mixer != null)
            _catalog.Mixer.SetFloat(_catalog.ExposedBgmParam, ToDecibel(volume));
    }
    
    public void SetSfxVolume01(float volume)
    {
        if (_catalog != null && _catalog.Mixer != null)
            _catalog.Mixer.SetFloat(_catalog.ExposedSfxParam, ToDecibel(volume));
    }

    public void SetAmbientVolume01(float volume)
    {
        if (_catalog != null && _catalog.Mixer != null)
            _catalog.Mixer.SetFloat(_catalog.ExposedAmbientParam, ToDecibel(volume));
    }

    private IEnumerator FadeInBgm(AudioClip clip, float sec, bool loop, float volume)
    {
        if (_bgmSource.isPlaying)
        {
            yield return FadeOut(_bgmSource, sec);
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.volume = 0;
        _bgmSource.gameObject.SetActive(true);
        _bgmSource.Play();

        float t = 0;
        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, volume, t / sec);
            yield return null;
        }
        _bgmSource.volume = volume;
    }

    private IEnumerator FadeOut(AudioSource source, float sec)
    {
        if (!source || !source.isPlaying || sec <= 0f)
        {
            if (source)
            {
                source.Stop();
                source.clip = null;
                yield break;
            }
        }

        float start = source.volume, t = 0f;

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, 0f, t / sec);
            yield return null;
        }
        source.Stop();
        source.clip = null;
    }

    private IEnumerator RecycleWhenDone(AudioSource source)
    {
        if (source.loop)
        {
            yield break;
        }

        yield return new WaitWhile(() => source && source.isPlaying);

        if (!source)
        {
            yield break;
        }

        source.gameObject.SetActive(false);
        _pool.Enqueue(source);
    }

    public AudioSource GetIdleSfxSource()
    {
        var src = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_sfxPrefab, transform);
        src.gameObject.SetActive(true);
        return src;
    }

    public void ReturnSfxSource(AudioSource source)
    {
        if (!source) return;
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        _pool.Enqueue(source);
    }

    public AudioClip GetSfxClip(ESfxId id)
    {
        if (_catalog.TryGet(id, out var entry))
            return entry.clip;
        return null;
    }
}
