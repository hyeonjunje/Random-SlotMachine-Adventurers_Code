using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class RestView : MonoBehaviour, IInitializable
{
    [SerializeField] private GameObject _pivotChangePool;
    [SerializeField] private GameObject _pivotFixedKeyword;
    [SerializeField] private SO_CharacterData[] _characterDatas;
    [SerializeField] private SlotInteractor[] _pivotHeros;
    [SerializeField] private Transform[] _pivotPoolHeros;

    private Camera _cam;
    private LayerMask _detectedLayer; // 감지될 레이어
    private Vector3 _dragOffset;

    /*private CharacterView _detectedPooledCharacterView; // 변경할 캐릭터로 감지된 캐릭터
    private Vector3 _detectedPooledCharacterOriginPos;  // 변경할 캐릭터의 원래 위치*/

    private CharacterView _detectedKeywordCharacterView; // 키워드를 바꿀 캐릭터

    private IDisposable _onEnterRestNodeEvent;
    private IDisposable _onArrangePlayerEvent;
    private IDisposable _onLeaveNodeEvent;

    public void Initialize()
    {
        _detectedLayer = 1 << LayerMask.NameToLayer("DropZone");
        _cam = Camera.main;
        _onEnterRestNodeEvent = EventBus.Subscribe<StEnterRestNodeEvent>(OnEnterRestNodeEvent);
    }

    private void OnDestroy()
    {
        _onEnterRestNodeEvent?.Dispose();
    }

    private void OnEnable()
    {
        _onArrangePlayerEvent = EventBus.Subscribe<StArrangePlayerEvent>(OnArrangePlayerEvent);
        _onLeaveNodeEvent = EventBus.Subscribe<StLeaveNodeEvent>(OnLeaveNodeEvent);
    }

    private void OnDisable()
    {
        _onArrangePlayerEvent?.Dispose();
        _onLeaveNodeEvent?.Dispose();
    }

    public void SetActiveChangeCharacter(bool flag)
    {
        _pivotChangePool.SetActive(flag);
        /*_detectedPooledCharacterView = null;
        _detectedPooledCharacterOriginPos = Vector3.zero;*/

        if (flag)
        {
            // Interactor 세팅
            for (int i = 0; i < _pivotHeros.Length; ++i)
            {
                CharacterView characterView = CharacterSystem.Instance.GetPlayer(i);
                _pivotHeros[i].SetInteractor(characterView, HandleBeginDrag_ChangeCharacter, HandleDrag_ChangeCharacter, HandleEndDrag_ChangeCharacter);
            }
        }
        else
        {
            foreach (SlotInteractor slotInteractor in _pivotHeros)
            {
                slotInteractor.Release();
            }
        }
    }

    public void SetActiveFixedCharacter(bool flag)
    {
        _pivotFixedKeyword.SetActive(flag);
        _detectedKeywordCharacterView = null;

        if (flag)
        {
            // Interactor 세팅
            for (int i = 0; i < _pivotHeros.Length; ++i)
            {
                CharacterView characterView = CharacterSystem.Instance.GetPlayer(i);
                _pivotHeros[i].SetInteractor(characterView, HandleBeginDrag_FixedKeyword, HandleDrag_FixedKeyword, HandleEndDrag_FixedKeyword);
            }
        }
        else
        {
            foreach (SlotInteractor slotInteractor in _pivotHeros)
            {
                slotInteractor.Release();
            }
        }
    }

    private void OnEnterRestNodeEvent(StEnterRestNodeEvent enterRestNodeEvent)
    {
        /*gameObject.SetActive(true);

        for (int i = 0; i < GameDefine.MAXPLAYERCOUNT; ++i)
        {
            CharacterView player = CharacterSystem.Instance.GetPlayer(i);
            if (player != null)
            {
                player.transform.SetParent(_pivotHeros[i].transform, false);
                player.transform.localPosition = Vector3.zero;
            }
        }

        // 변경할 영웅들 미리 세팅
        OfferedCharacterData[] changeCharacterPool = new OfferedCharacterData[GameDefine.MAXCHANGEPLAYERCOUNT];

        for (int i = 0; i < changeCharacterPool.Length; ++i)
        {
            SO_CharacterData pickedCharacterData = _characterDatas.GetRandomElement();
            changeCharacterPool[i] = new OfferedCharacterData(pickedCharacterData);
        }

        // 교환될 캐릭터 4개 생성
        for (int i = 0; i < _pivotPoolHeros.Length; ++i)
        {
            CharacterView characterView = Creator.Instance.CreatAsset<CharacterView>(ECreatorAsset.CharacterView, Vector3.zero, Quaternion.identity);
            Character character = new Character(changeCharacterPool[i].CharacterData, EBattleSideType.OurSide);
            characterView.Setup(character);
            _pivotPoolHeros[i].DestroyAllChildren();
            characterView.transform.SetParent(_pivotPoolHeros[i], false);
        }*/
    }

    private void OnArrangePlayerEvent(StArrangePlayerEvent arrangePlayerEvent)
    {
        // 내 캐릭터 세팅
        for (int i = 0; i < GameDefine.MAXPLAYERCOUNT; ++i)
        {
            CharacterView player = CharacterSystem.Instance.GetPlayer(i);
            if (player != null)
            {
                player.transform.SetParent(_pivotHeros[i].transform, false);
                player.transform.localPosition = Vector3.zero;
            }
        }
    }

    private void OnLeaveNodeEvent(StLeaveNodeEvent leaveNodeEvent)
    {
        gameObject.SetActive(false);
    }

    private void HandleBeginDrag_ChangeCharacter(object slot, PointerEventData eventData)
    {
        if(slot is CharacterView characterView)
        {
            Vector3 originSlotPos = characterView.transform.position;
            Vector3 dragPosition = _cam.ScreenToWorldPoint(eventData.position);
            dragPosition.z = originSlotPos.z;
            _dragOffset = originSlotPos - dragPosition;
        }
    }

    private void HandleDrag_ChangeCharacter(object slot, PointerEventData eventData)
    {
        /*if (slot is CharacterView characterView)
        {
            Vector3 dragPosition = _cam.ScreenToWorldPoint(eventData.position);
            characterView.transform.position = new Vector3(dragPosition.x, dragPosition.y, characterView.transform.position.z) + _dragOffset;

            Collider2D hit = Physics2D.OverlapPoint(dragPosition, _detectedLayer);

            if (hit != null)
            {
                if(_detectedPooledCharacterView == null || _detectedPooledCharacterView.gameObject != hit.gameObject)
                {
                    // 이전 캐릭터 처리
                    if(_detectedPooledCharacterView)
                    {
                        _detectedPooledCharacterView.transform.position = _detectedPooledCharacterOriginPos;
                    }

                    _detectedPooledCharacterView = hit.GetComponentInChildren<CharacterView>();
                    _detectedPooledCharacterOriginPos = _detectedPooledCharacterView.transform.position;
                    _detectedPooledCharacterView.transform.position = _pivotHeros[characterView.Character.PosIndex].transform.position; ;
                }
            }
            else
            {
                if (_detectedPooledCharacterView)
                {
                    _detectedPooledCharacterView.transform.position = _detectedPooledCharacterOriginPos;
                    _detectedPooledCharacterView = null;
                }
            }
        }*/
    }

    private void HandleEndDrag_ChangeCharacter(object slot, PointerEventData eventData)
    {
        /*if (slot is CharacterView characterView)
        {
            if(_detectedPooledCharacterView)
            {
                _detectedPooledCharacterView.Character.SetPosIndex(characterView.Character.PosIndex);
                DespawnPlayerGA despawnPlayerGA = new DespawnPlayerGA(characterView);
                SpawnPlayerGA spawnPlayerGA = new SpawnPlayerGA(_detectedPooledCharacterView.Character);

                ActionSystem.Instance.Perform(despawnPlayerGA, () =>
                {
                    ActionSystem.Instance.Perform(spawnPlayerGA, () =>
                    {
                        Creator.Instance.RemoveAsset(ECreatorAsset.CharacterView, _detectedPooledCharacterView.gameObject);

                        UI_Rest uiRest = UIManager.Instance.Get<UI_Rest>(EUIType.UI_Rest);
                        uiRest.ActiveClearButton();
                    });
                });
            }
            else
            {
                characterView.transform.position = _pivotHeros[characterView.Character.PosIndex].transform.position; ;
            }
        }*/
    }

    private void HandleBeginDrag_FixedKeyword(object slot, PointerEventData eventData)
    {
        /*if (slot is CharacterView characterView)
        {
            Vector3 originSlotPos = characterView.transform.position;
            Vector3 dragPosition = _cam.ScreenToWorldPoint(eventData.position);
            dragPosition.z = originSlotPos.z;
            _dragOffset = originSlotPos - dragPosition;

            if(characterView == _detectedKeywordCharacterView)
            {
                UI_Rest uiRest = UIManager.Instance.Get<UI_Rest>(EUIType.UI_Rest);
                _detectedKeywordCharacterView = null;
                uiRest.OnRegisterFixedCharacter(_detectedKeywordCharacterView);
            }
        }*/
    }

    private void HandleDrag_FixedKeyword(object slot, PointerEventData eventData)
    {
        if (slot is CharacterView characterView)
        {
            Vector3 dragPosition = _cam.ScreenToWorldPoint(eventData.position);
            characterView.transform.position = new Vector3(dragPosition.x, dragPosition.y, characterView.transform.position.z) + _dragOffset;
        }
    }

    private void HandleEndDrag_FixedKeyword(object slot, PointerEventData eventData)
    {
        if (slot is CharacterView characterView)
        {
            Vector3 dragPosition = _cam.ScreenToWorldPoint(eventData.position);
            Collider2D hit = Physics2D.OverlapPoint(dragPosition, _detectedLayer);

            if (hit != null)
            {
                if (_detectedKeywordCharacterView != null && _detectedKeywordCharacterView != characterView)
                {
                    // _detectedKeywordCharacterView.transform.position = _pivotHeros[_detectedKeywordCharacterView.Character.PosIndex].transform.position;
                }
                _detectedKeywordCharacterView = characterView;

                UI_Rest uiRest = UIManager.Instance.Get<UI_Rest>(EUIType.UI_Rest);

                _detectedKeywordCharacterView.transform.position = hit.transform.position;
            }
            else
            {
                if(characterView == _detectedKeywordCharacterView)
                {
                    UI_Rest uiRest = UIManager.Instance.Get<UI_Rest>(EUIType.UI_Rest);
                    _detectedKeywordCharacterView = null;
                }

                // characterView.transform.position = _pivotHeros[characterView.Character.PosIndex].transform.position;
            }
        }
    }
}
