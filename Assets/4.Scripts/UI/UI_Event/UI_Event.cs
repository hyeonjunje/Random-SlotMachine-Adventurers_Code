using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Event : UI_Base
{
    [SerializeField] private GameObject _pivotEventInfo;
    [SerializeField] private TMP_Text _textTitle;
    [SerializeField] private Image _imageEvent; 
    [SerializeField] private TMPEffectController _textExplain;
    [SerializeField] private ListItem_EventChoice[] _choices;
    
    [field: SerializeField] public EventSlotMachineController SlotMachineController { get; private set; }

    private SO_EventData _eventData;

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    public override void Open()
    {
        gameObject.SetActive(true);
    }

    public void Setup(SO_EventData eventData)
    {
        ActivePage(true);

        _eventData = eventData;
        _textTitle.text = LocalizationManager.Instance.Get(eventData.EventName);

        foreach (PageData pageData in _eventData.PageDatas)
        {
            if (pageData.IsStartPage)
            {
                SetPage(pageData);
                break;
            }
        }

        // 슬롯머신 미니게임
        if(eventData.MiniGameType == EMiniGameType.StartingSlotMachine)
        {
            // 컨트롤러(오브젝트) 활성화가 필요하다면
            SlotMachineController.SetMiniGameSlotMachine();
        }
    }

    public void SetPage(int id)
    {
        foreach (PageData pageData in _eventData.PageDatas)
        {
            if(pageData.Id == id)
            {
                SetPage(pageData);
                break;
            }
        }
    }

    // 페이지 정보를 활성화, 비활성화할지 (미니게임할 때 가려주기 위함)
    public void ActivePage(bool flag)
    {
        _pivotEventInfo.SetActive(flag);
    }

    private void SetPage(PageData pageData)
    {
        _textExplain.SetText(LocalizationManager.Instance.Get(pageData.EventExplain));

        _imageEvent.gameObject.SetActive(pageData.EventSprite != null);
        _imageEvent.sprite = pageData.EventSprite;

        for (int i = 0; i < _choices.Length; ++i)
        {
            if(i < pageData.Choices.Length)
            {
                _choices[i].SetListItem(pageData.Choices[i]);
            }
            else
            {
                _choices[i].Release();
            }
        }
    }
}
