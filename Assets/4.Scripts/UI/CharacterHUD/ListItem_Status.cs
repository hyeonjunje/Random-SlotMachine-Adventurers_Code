using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListItem_Status : BaseListItem<Status>
{
    [SerializeField] private Image _imageStatus;
    [SerializeField] private TMP_Text _textStatusTurn;


    [Header("애니메이션 효과")]
    [SerializeField] private float _duration = 0.2f;

    [SerializeField] private Ease _addEase;
    [SerializeField] private Ease _updateEase;
    [SerializeField] private Ease _removeEase;

    private Tweener _statusTweener;

    // 생성
    public override void SetListItem(Status item)
    {
        base.SetListItem(item);
        gameObject.SetActive(true);
        _imageStatus.sprite = item.StatusSprite;
        _textStatusTurn.text = item.RemainTurn.ToString();

        if (_statusTweener != null)
        {
            _statusTweener.Kill();
        }
        transform.localScale = Vector3.zero;
        _statusTweener = transform.DOScale(Vector3.one, _duration).SetEase(_addEase);
    }

    // 업데이트
    public void Refresh()
    {
        gameObject.SetActive(true);
        _imageStatus.sprite = Item.StatusSprite;
        _textStatusTurn.text = Item.RemainTurn.ToString();

        if (_statusTweener != null)
        {
            _statusTweener.Kill();
        }
        transform.localScale = Vector3.one;
        _statusTweener = transform.DOPunchScale(Vector3.one * 0.3f, _duration, 1, 1).SetEase(_updateEase);
    }

    // 소멸
    public void Release()
    {
        if (_statusTweener != null)
        {
            _statusTweener.Kill();
        }
        _statusTweener = transform.DOScale(Vector3.zero, _duration).SetEase(_removeEase)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
