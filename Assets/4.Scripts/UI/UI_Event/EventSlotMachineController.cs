using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class EventSlotMachineController : MonoBehaviour
{
    [SerializeField] private SlotMachineReel[] _reels;
    [SerializeField] private SO_SlotMachineConfig _slotMachineConfig;
    [SerializeField] private SO_MiniGameSlotMachineConfig _miniGameSlotMachineConfig;
    [SerializeField] private float _spinTime = 1.5f;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Coroutine _coSequence;
    private Action<EEventSlotMachineKeyword> _onComplete;

    private readonly int PullAnimationHash = Animator.StringToHash("Pull");

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (AppConfig.IsCheatEnabled && AppConfig.BootStrapperType == EBootstrapperType.Custom1)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayStartSequence(null);
            }
        }
    }

    public void SetMiniGameSlotMachine()
    {
        _canvasGroup.alpha = 1;
        gameObject.SetActive(true);

        // 초기화 및 무작위 더미 아이콘 세팅
        foreach (var reel in _reels)
        {
            reel.SetEventConfig(_slotMachineConfig, _miniGameSlotMachineConfig);
            reel.SetEventReel();
        }
    }

    public void HideMiniGameSlotMachine(Action onFinishHideSlotMachine)
    {
        _canvasGroup.DOFade(0, 1f).OnComplete(() =>
        {
            onFinishHideSlotMachine?.Invoke();
            gameObject.SetActive(false);
        });
    }

    public void PlayStartSequence(Action<EEventSlotMachineKeyword> onComplete)
    {
        // 코루틴 중단 및 OnComplete 캐싱
        if (_coSequence != null) StopCoroutine(_coSequence);
        _onComplete = onComplete;

        // 레버 댕기기 애니메이션
        _animator.SetTrigger(PullAnimationHash);
    }

    public int GetRewardValue(EEventSlotMachineKeyword eventSlotMachineKeyword)
    {
        SlotMachineKeywordConfig slotMachineKeyword = _miniGameSlotMachineConfig.GetConfigByKeyword(eventSlotMachineKeyword);
        if(slotMachineKeyword != null)
        {
            return slotMachineKeyword.RewardValue;
        }
        return 0;
    }

    // 애니메이션 키 이벤트로 인해 호출되는 메소드 (SlotMachine_Event_Pull 애니메이션에 바인딩)
    private void PlayStartSequence_Animation()
    {
        _coSequence = StartCoroutine(CoSequence(_onComplete));
    }

    private IEnumerator CoSequence(Action<EEventSlotMachineKeyword> onComplete)
    {
        // 스핀 시작
        for (int i = 0; i < _reels.Length; i++)
        {
            StartCoroutine(_reels[i].CoSpinEvent(i * 0.15f));
        }

        yield return new WaitForSeconds(_spinTime);

        // 랜덤한 당첨 키워드 뽑기 (모두 같은 결과)
        EEventSlotMachineKeyword resultKeyword = _miniGameSlotMachineConfig.GetRandomKeyword();
        EEventSlotMachineKeyword[] results = { resultKeyword, resultKeyword, resultKeyword };

        // 스핀 멈추기
        for (int i = 0; i < _reels.Length; i++)
        {
            if (i == _reels.Length - 1)
                yield return StartCoroutine(_reels[i].CoStopEvent(results[i], 0.2f));
            else
            {
                _reels[i].StopEvent(results[i], 0.2f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        yield return new WaitForSeconds(1.0f);
        
        // 끝나면 당첨된 키워드와 함께 액션 호출
        onComplete?.Invoke(resultKeyword);
    }
}
