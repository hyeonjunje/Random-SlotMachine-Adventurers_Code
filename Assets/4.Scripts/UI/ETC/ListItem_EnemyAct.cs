using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ListItem_EnemyAct : BaseListItem<Enemy>
{
    public RectTransform RectTrans { get; private set; }

    [SerializeField] private Image _imagePortrait;

    [SerializeField] private Vector2 _offset = new Vector2(-20, 10);
    [SerializeField] private float _spacing = 100f;

    private Vector2 _initPos;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        RectTrans = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        _initPos = Vector3.left * transform.parent.GetComponent<RectTransform>().sizeDelta.x / 2;
    }

    public override void SetListItem(Enemy item)
    {
        base.SetListItem(item);

        gameObject.SetActive(true);
        _imagePortrait.sprite = SpriteManager.Instance.GetSprite(item.EnemyData.PortraitIconName);

        RectTrans.anchoredPosition = _initPos;
        _canvasGroup.alpha = 0;
        Tween tween = _canvasGroup.DOFade(1, StyleManager.Instance.AnimationTimeData.AppearTokenTime).
            SetEase(Ease.Linear);
    }

    public IEnumerator CoRelease()
    {
        Tween tween = _canvasGroup.DOFade(0, StyleManager.Instance.AnimationTimeData.DisappearTokenTime).
            SetEase(Ease.Linear).
            OnComplete(() => Destroy(gameObject));

        yield return tween.WaitForCompletion();
    }

    public void SetPos(int index)
    {
        RectTrans.anchoredPosition = _offset + Vector2.left * _spacing * index;
    }
}
