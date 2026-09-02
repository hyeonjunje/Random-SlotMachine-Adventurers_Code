using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SlotMachineReel : MonoBehaviour
{
    [SerializeField] private SlotMachineSlot[] _slots;
    [SerializeField] private ParticleSystem _highlightParticle;
    [SerializeField] private ParticleSystem _blinkParticle;

    private SO_SlotMachineConfig _config;

    private List<EKeyword> _keywords = new List<EKeyword>();
    private int _index;
    private Vector2[] _slotOriginRectPos;
    private float _slotHeight;

    private int _currentCenterIndex = 0; // 돌아가는중에 가장 가운데 슬롯의 인덱스
    private int _targetCenterIndex = 0;  // 내가 멈출 슬롯의 인덱스
    private Coroutine _coSpin;

    private bool _isInit = false;

    private AudioSource _audioSource;
    private float _lastTickTime = 0f;
    private const float TICK_COOLDOWN = 0.1f;
    private float[] _prevSlotYPos;

    private void Update()
    {
        if (_audioSource != null)
        {
            TryPlayTickSound();
        }
    }

    private void OnEnable()
    {
        _highlightParticle.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReturnAudioSource();
    }

    private void Init()
    {
        if (_isInit) return;
        
        _slotOriginRectPos = new Vector2[_slots.Length];

        for (int i = 0; i < _slots.Length; ++i)
        {
            _slotOriginRectPos[i] = _slots[i].Rect.anchoredPosition;
        }

        _slotHeight = _slots[0].GetComponent<RectTransform>().sizeDelta.y;
        _isInit = true;
    }

    public void SetConfig(SO_SlotMachineConfig config)
    {
        _config = config;
    }
    public void SetTitleConfig(SO_SlotMachineConfig config)
    {
        Init ();
        _config = config;
    }

    public void StopTitle(ETitleKeyword result, float delay)
    {
        StartCoroutine (CoStopTitle (result, delay));
    }

    public IEnumerator CoSpinTitle(float delay)
    {
        yield return new WaitForSeconds (delay);
        RentAudioSource(ESfxId.TitleSlotMachineSpin);
        _coSpin = StartCoroutine (CoSpinUpdate ());
    }

    private IEnumerator CoSpinUpdate()
    {
        for (int i = 0; i < _slots.Length; ++i)
            _slots[i].Contents.localScale = _config.SlotSpinScale;

        while (true)
        {
            float highestPosY = -1000;
            for (int i = 0; i < _slots.Length; ++i)
            {
                _slots[i].Rect.anchoredPosition += Vector2.down * _config.SlotMoveSpeed * Time.deltaTime;
                highestPosY = Mathf.Max (highestPosY, _slots[i].Rect.anchoredPosition.y);
            }

            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i].Rect.anchoredPosition.y < _config.SlotRestoreYPos)
                {
                    _slots[i].Rect.anchoredPosition = new Vector2 (_slots[i].Rect.anchoredPosition.x, highestPosY + _slotHeight);
                }
            }

            float centerYPos = Mathf.Infinity;
            for (int i = 0; i < _slots.Length; ++i)
            {
                if (centerYPos > Mathf.Abs (_slots[i].Rect.anchoredPosition.y))
                {
                    centerYPos = Mathf.Abs (_slots[i].Rect.anchoredPosition.y);
                    _currentCenterIndex = i;
                }
            }
            
            yield return null;
        }
    }

    public IEnumerator CoStopTitle(ETitleKeyword result, float delay)
    {
        yield return new WaitForSeconds (delay);

        _targetCenterIndex = (_currentCenterIndex + _config.StopOffset) % _slots.Length;

        _slots[_targetCenterIndex].SetText (result.ToString ());

        while (true)
        {
            if (_slots[_targetCenterIndex].Rect.anchoredPosition.y <= _slotOriginRectPos[0].y)
            {
                if (_coSpin != null) StopCoroutine (_coSpin);
                break;
            }
            yield return null;
        }

        for (int i = 0; i < _slots.Length; ++i)
        {
            int index = (i + _targetCenterIndex) % _slots.Length;
            if (index == _targetCenterIndex)
                _slots[index].Rect.DOAnchorPos (_slotOriginRectPos[i] + Vector2.down * _config.SlotExtraUnderDampingYPos, _config.UnderDampingDuration).SetEase (_config.DampingEase);
            else
            {
                _slots[index].Rect.anchoredPosition = _slotOriginRectPos[i];
                _slots[index].Contents.localScale = Vector3.one;
            }
        }

        yield return new WaitForSeconds (_config.UnderDampingDuration);
        _slots[_targetCenterIndex].Rect.DOAnchorPos (_slotOriginRectPos[0], _config.RestoreDuration).SetEase (_config.RestoreEase);
        yield return new WaitForSeconds (_config.RestoreDuration - _config.CrossRestoreDuration);
        _slots[_targetCenterIndex].Contents.DOScale (Vector3.one, _config.SizeRestoreDuration).SetEase (_config.SizeRestoreEase);
        
        yield return new WaitForSeconds(_config.SizeRestoreDuration);
        ReturnAudioSource();
    }

    #region Event MiniGame Routine
    [Header("Event Slot Machine")]
    [SerializeField, Tooltip("정답(결과) 기호가 멈출 슬롯의 인덱스입니다. (예: 두 번째 줄에 멈추길 원하면 1 입력)")] 
    private int _eventTargetVisualIndex = 0;
    
    private Coroutine _coStopEvent;
    private SO_MiniGameSlotMachineConfig _eventConfig;

    public void SetEventConfig(SO_SlotMachineConfig config, SO_MiniGameSlotMachineConfig eventConfig)
    {
        Init();
        _config = config;
        _eventConfig = eventConfig;
    }

    public void SetEventReel()
    {
        Init();

        // 찌꺼기 코루틴/트윈 정리 (버그 방지)
        if (_coSpin != null) StopCoroutine(_coSpin);
        if (_coStopEvent != null) StopCoroutine(_coStopEvent);

        if (_eventConfig != null && _eventConfig.KeywordConfigs != null)
        {
            List<Sprite> icons = new List<Sprite>();
            foreach (var config in _eventConfig.KeywordConfigs)
            {
                if (config.Icon != null)
                {
                    icons.Add(config.Icon);
                }
            }

            if (icons.Count > 0)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    _slots[i].Rect.DOKill();
                    _slots[i].Contents.DOKill();

                    _slots[i].Rect.anchoredPosition = _slotOriginRectPos[i];
                    _slots[i].Contents.localScale = Vector3.one;

                    Sprite randomSpt = icons[Random.Range(0, icons.Count)];
                    _slots[i].SetSprite(randomSpt);
                }
            }
        }
    }

    public void StopEvent(EEventSlotMachineKeyword result, float delay)
    {
        if (_coStopEvent != null) StopCoroutine(_coStopEvent);
        _coStopEvent = StartCoroutine(CoStopEvent(result, delay));
    }

    public IEnumerator CoSpinEvent(float delay)
    {
        if (_coSpin != null) StopCoroutine(_coSpin);
        yield return new WaitForSeconds(delay);
        if (_coSpin != null) StopCoroutine(_coSpin);
        RentAudioSource(ESfxId.EventSlotMachineSpin);
        _coSpin = StartCoroutine(CoSpinUpdate());
    }

    public IEnumerator CoStopEvent(EEventSlotMachineKeyword result, float delay)
    {
        yield return new WaitForSeconds(delay);

        _targetCenterIndex = (_currentCenterIndex + _config.StopOffset) % _slots.Length;

        if (_eventConfig != null)
        {
            var keywordData = _eventConfig.GetConfigByKeyword(result);
            if (keywordData != null && keywordData.Icon != null)
            {
                _slots[_targetCenterIndex].SetSprite(keywordData.Icon);
            }
        }

        int targetOffset = _eventTargetVisualIndex % _slots.Length;

        while (true)
        {
            if (_slots[_targetCenterIndex].Rect.anchoredPosition.y <= _slotOriginRectPos[targetOffset].y)
            {
                if (_coSpin != null) StopCoroutine(_coSpin);
                break;
            }
            yield return null;
        }

        // 정답 슬롯이 배열의 targetOffset 번째 위치에 가도록 인덱스를 매핑하고 이벤트용은 모든 슬롯에 댐핑을 줍니다.
        for (int i = 0; i < _slots.Length; ++i)
        {
            int index = (i - targetOffset + _targetCenterIndex + _slots.Length) % _slots.Length;
            
            // 모든 슬롯 다같이 댐핑
            _slots[index].Rect.DOAnchorPos(_slotOriginRectPos[i] + Vector2.down * _config.SlotExtraUnderDampingYPos, _config.UnderDampingDuration).SetEase(_config.DampingEase);
            
            if (index != _targetCenterIndex)
                _slots[index].Contents.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(_config.UnderDampingDuration);
        
        // 전체 애니메이션 복구
        for (int i = 0; i < _slots.Length; ++i)
        {
            int index = (i - targetOffset + _targetCenterIndex + _slots.Length) % _slots.Length;
            _slots[index].Rect.DOAnchorPos(_slotOriginRectPos[i], _config.RestoreDuration).SetEase(_config.RestoreEase);
        }
        
        yield return new WaitForSeconds(_config.RestoreDuration - _config.CrossRestoreDuration);
        _slots[_targetCenterIndex].Contents.DOScale(Vector3.one, _config.SizeRestoreDuration).SetEase(_config.SizeRestoreEase);
        
        yield return new WaitForSeconds(_config.SizeRestoreDuration);
        ReturnAudioSource();
    }
    #endregion

    public void SetReel(IReadOnlyList<EKeyword> keywords, int index)
    {
        Init();

        _index = index;

        _keywords.Clear();
        _keywords.AddRange(keywords);
        _keywords.Shuffle();

        for (int i = 0; i < _slots.Length; ++i)
        {
            _slots[i].Rect.anchoredPosition = _slotOriginRectPos[i];
            _slots[i].SetText(_keywords[i % _keywords.Count]);
        }
    }

    public void Spin(float delay)
    {
        _coSpin = StartCoroutine(CoSpin(delay));
    }

    public void Stop(EKeyword result, float delay)
    {
        StartCoroutine(CoStop(result, delay));
    }

    public IEnumerator CoSpin(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 내려갈 때 살짝 길게 늘려준다.
        for (int i = 0; i < _slots.Length; ++i)
        {
            _slots[i].Contents.localScale = _config.SlotSpinScale;
        }

        RentAudioSource(ESfxId.SlotMachineSpin);

        while (true)
        {
            float heightestPosY = 0;

            for (int i = 0; i < _slots.Length; ++i)
            {
                _slots[i].Rect.anchoredPosition += Vector2.down * _config.SlotMoveSpeed * Time.deltaTime;

                heightestPosY = Mathf.Max(heightestPosY, _slots[i].Rect.anchoredPosition.y);
            }

            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i].Rect.anchoredPosition.y < _config.SlotRestoreYPos)
                {
                    _slots[i].Rect.anchoredPosition = new Vector2(_slots[i].Rect.anchoredPosition.x, heightestPosY + _slotHeight);
                }
            }

            float centerYPos = Mathf.Infinity; 
            for (int i = 0; i < _slots.Length; ++i)
            {
                if(centerYPos > Mathf.Abs(_slots[i].Rect.anchoredPosition.y))
                {
                    centerYPos = Mathf.Abs(_slots[i].Rect.anchoredPosition.y);
                    _currentCenterIndex = i;
                }
            }

            yield return null;
        }
    }

    public IEnumerator CoStop(EKeyword result, float delay)
    {
        yield return new WaitForSeconds(delay);

        _targetCenterIndex = (_currentCenterIndex + _config.StopOffset) % _slots.Length;
        _slots[_targetCenterIndex].SetText(result);

        while (true)
        {
            if(_slots[_targetCenterIndex].Rect.anchoredPosition.y <= _slotOriginRectPos[0].y)
            {
                StopCoroutine(_coSpin);
                break;
            }

            yield return null;
        }

        // 살짝 더 가는 효과
        for (int i = 0; i < _slots.Length; ++i)
        {
            int index = (i + _targetCenterIndex) % _slots.Length;

            if(index == _targetCenterIndex)
            {
                _slots[index].Rect.DOAnchorPos(_slotOriginRectPos[i] + Vector2.down * _config.SlotExtraUnderDampingYPos, _config.UnderDampingDuration).SetEase(_config.DampingEase);
            }
            else
            {
                _slots[index].Rect.anchoredPosition = _slotOriginRectPos[i];
                _slots[index].Contents.localScale = Vector3.one;
            }
            
        }

        yield return new WaitForSeconds(_config.UnderDampingDuration);

        // 위치 보정
        _slots[_targetCenterIndex].Rect.DOAnchorPos(_slotOriginRectPos[0], _config.RestoreDuration).SetEase(_config.RestoreEase);

        yield return new WaitForSeconds(_config.RestoreDuration - _config.CrossRestoreDuration);

        // 크기도 다시 원복해준다.
        _slots[_targetCenterIndex].Contents.DOScale(Vector3.one, _config.SizeRestoreDuration).SetEase(_config.SizeRestoreEase);

        yield return new WaitForSeconds(_config.SizeRestoreDuration);
        ReturnAudioSource();
    }

    // 현재 멈춰있는 슬롯의 텍스트를 수정한다.
    public void SetCurrentSlotText(EKeyword slotMachineKeyword)
    {
        _slots[_targetCenterIndex].SetText(slotMachineKeyword);
    }

    public void PlayHighlight()
    {
        _highlightParticle.gameObject.SetActive(false);
        _highlightParticle.gameObject.SetActive(true);
    }

    public void BlinkHighlight()
    {
        _blinkParticle.gameObject.SetActive(false);
        _blinkParticle.gameObject.SetActive(true);
    }

    public void StopBlinkHighlight()
    {
        _blinkParticle.gameObject.SetActive(false);
    }

    #region UIEvent
    public void OnClickReel()
    {
        UI_SelectionContext uiSelectionContext = UIManager.Instance.Get<UI_SelectionContext>(EUIType.UI_SelectionContext);
        if(uiSelectionContext.IsControlled(transform))
        {
            uiSelectionContext.AddIndex(_index);
        }
    }
    #endregion

    private void RentAudioSource(ESfxId sfxId)
    {
        if (_audioSource == null && AudioManager.Instance != null)
        {
            _audioSource = AudioManager.Instance.GetIdleSfxSource();
            _audioSource.clip = AudioManager.Instance.GetSfxClip(sfxId);
        }
    }

    private void ReturnAudioSource()
    {
        if (_audioSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.ReturnSfxSource(_audioSource);
            _audioSource = null;
        }
    }

    private void TryPlayTickSound()
    {
        if (_audioSource == null) return;

        if (_prevSlotYPos == null || _prevSlotYPos.Length != _slots.Length)
        {
            _prevSlotYPos = new float[_slots.Length];
            for (int i = 0; i < _slots.Length; ++i)
                _prevSlotYPos[i] = _slots[i].transform.localPosition.y;
            return;
        }

        bool shouldTick = false;
        for (int i = 0; i < _slots.Length; ++i)
        {
            float currentY = _slots[i].transform.localPosition.y;
            if (_prevSlotYPos[i] >= -0.01f && currentY < -0.01f)
            {
                shouldTick = true;
            }
            _prevSlotYPos[i] = currentY;
        }

        if (shouldTick && Time.time - _lastTickTime > TICK_COOLDOWN)
        {
            _audioSource.Play();
            _lastTickTime = Time.time;
        }
    }
}
