using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class ArtifactPopup : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private CanvasGroup _canvasGroup;

    public void PlayPopupEffect(Sprite iconSprite, Action onComplete = null)
    {
       /* _iconImage.sprite = iconSprite;

        RectTransform rt = GetComponent<RectTransform> ();

        rt.anchoredPosition = Vector2.zero; 
        rt.localScale = Vector3.zero;      
        rt.localRotation = Quaternion.identity; 
        _canvasGroup.alpha = 1f;

        Sequence seq = DOTween.Sequence ();

        seq.Append (transform.DOScale (Vector3.one * 3.0f, 0.25f).SetEase (Ease.OutBack));

        seq.Join(transform.DORotate(new Vector3(0, 0, 360), 0.25f, RotateMode.FastBeyond360));

        seq.Append (transform.DOPunchScale (Vector3.one * 0.5f, 0.2f, 10, 1));

        seq.AppendInterval (0.5f);

        seq.Append (_canvasGroup.DOFade (0f, 0.3f));
        seq.Join (transform.DOScale (Vector3.one * 3.5f, 0.3f));

        seq.OnComplete (() =>
        {
            onComplete?.Invoke();
            Destroy (gameObject);
        });*/
    }

    private void OnDestroy()
    {
        transform.DOKill ();
    }
}
