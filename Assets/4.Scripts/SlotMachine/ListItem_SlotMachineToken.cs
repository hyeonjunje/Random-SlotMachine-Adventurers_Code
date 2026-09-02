using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_SlotMachineToken : BaseListItem<BattleAct>, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    public RectTransform RectTrans { get; private set; }
    public Action<ListItem_SlotMachineToken> OnDragStarted;
    public Action<ListItem_SlotMachineToken, Vector2> OnDragMoved;
    public Action<ListItem_SlotMachineToken> OnDragEnded;

    [SerializeField] private Image _imageToken;
    [SerializeField] private Image _imageIcon;

    private Vector2 _initPos;
    private CanvasGroup _canvasGroup;
    private int _bingoIndex = 0;
    private bool _isDrag = false;
    private bool _isHover = false;

    private void Awake()
    {
        RectTrans = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        _initPos = Vector3.left * transform.parent.GetComponent<RectTransform>().sizeDelta.x / 2;
    }

    private void OnDisable()
    {
        if(_isHover)
        {
            _isHover = false;

            UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
            uiBattle.HideCardPreview(Item);

            SlotMachineViewer uiSlotMachine = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
            if (Item.Skill.IsClickableSkill)
            {
                uiSlotMachine.StopBlinkHighlight();
            }

            Item.CharacterView.HoverCharacter(false);
        }
    }

    public override void SetListItem(BattleAct item)
    {
        base.SetListItem(item);

        gameObject.SetActive(true);
        
        if(item.IsPlayer)
        {
            if(item.Skill.IsClickableSkill)
            {
                _imageToken.sprite = SpriteManager.Instance.GetSprite("Token_Clickable");
                _imageToken.color = StyleManager.Instance.GetColor(EColorKey.Token_Clickable);
            }
            else
            {
                _imageToken.sprite = SpriteManager.Instance.GetSprite("Token_Normal");
                _imageToken.color = StyleManager.Instance.GetColor(EColorKey.Token_Normal);
            }
        }
        else
        {
            _imageToken.sprite = SpriteManager.Instance.GetSprite("Token_Enemy");
            _imageToken.color = StyleManager.Instance.GetColor(EColorKey.Token_Enemy);
        }

        _imageIcon.sprite = SpriteManager.Instance.GetSprite(item.CharacterView.Character.CharacterData.SubjectIconName);

        RectTrans.anchoredPosition = _initPos;
        _canvasGroup.alpha = 0;
        Tween tween = _canvasGroup.DOFade(1, StyleManager.Instance.AnimationTimeData.AppearTokenTime).
            SetEase(Ease.Linear);

        // 이펙트 재생
        SlotMachineViewer uiSlotMachine = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        uiSlotMachine.PlayBingoHighlight(_bingoIndex);
    }

    public IEnumerator CoRelease()
    {
        OnDragStarted = null;
        OnDragMoved = null;
        OnDragEnded = null;

        Tween tween = _canvasGroup.DOFade(0, StyleManager.Instance.AnimationTimeData.DisappearTokenTime).
            SetEase(Ease.Linear).
            OnComplete(() => Destroy(gameObject));

        yield return tween.WaitForCompletion();
    }

    public void Release()
    {
        _canvasGroup.DOFade(0, StyleManager.Instance.AnimationTimeData.DisappearTokenTime).
            SetEase(Ease.Linear).
            OnComplete(() => Destroy(gameObject));
    }

    public void SetBingoIndex(int bingoIndex)
    {
        _bingoIndex = bingoIndex;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.ShowCardPreview(Item);

        SlotMachineViewer uiSlotMachine = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);

        List<int> slotIndexes = new List<int>();
        foreach (Keyword keyword in Item.Skill.ClickableKeywords)
        {
            slotIndexes.Add(keyword.SlotIndex);
        }
        uiSlotMachine.BlinkHighlight(slotIndexes);

        // 캐릭터 호버 기능
        Item.CharacterView.HoverCharacter(true);

        _isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        uiBattle.HideCardPreview(Item);

        SlotMachineViewer uiSlotMachine = UIManager.Instance.Get<SlotMachineViewer>(EUIType.UI_SlotMachine);
        if(Item.Skill.IsClickableSkill)
        {
            uiSlotMachine.StopBlinkHighlight();
        }

        // 캐릭터 호버 종료 기능
        Item.CharacterView.HoverCharacter(false);

        _isHover = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Item.IsPlayer == false)
        {
            return;
        }

        transform.position = new Vector3(eventData.position.x, transform.position.y, transform.position.z);
        OnDragMoved?.Invoke(this, eventData.position);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item.IsPlayer == false)
        {
            return;
        }

        OnDragStarted?.Invoke(this);
        _isDrag = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Item.IsPlayer == false)
        {
            return;
        }

        OnDragEnded?.Invoke(this);
        _isDrag = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Item.IsPlayer == false)
        {
            return;
        }

        if (_isDrag == false)
        {
            if(Item.Skill.IsClickableSkill)
            {
                if(BattleSystem.Instance.BattleState == EBattleState.SelectTarget)
                {
                    return;
                }

                if (ManaSystem.Instance.CanSpend(Item.Skill.ManaCost))
                {
                    ActionSystem.Instance.Perform(new SpendManaGA(Item.Skill.ManaCost), () =>
                    {
                        Debug.Log("클릭으로 인해 발동되는 토큰");
                        ClickUseSlotMachineTokenGA clickUseSlotMachineTokenGA = new ClickUseSlotMachineTokenGA(Item);
                        ActionSystem.Instance.Perform(clickUseSlotMachineTokenGA);
                    });
                }
                else
                {
                    ManaSystem.Instance.ShowManaShortagegMessage();
                }
            }
        }
    }
}
