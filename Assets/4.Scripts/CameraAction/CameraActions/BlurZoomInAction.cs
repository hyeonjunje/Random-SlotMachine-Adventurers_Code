using DG.Tweening;
using UnityEngine;

public class BlurZoomInAction : CameraAction
{
    [SerializeField] private float _targetZoomInSize = 4f;
    [SerializeField] private Vector3 _targetPos = new Vector3(0, -0.5f, 0);
    [SerializeField] private float _zoomInDuration = 0.1f;
    [SerializeField] private Ease _zoomInEase = Ease.OutCubic;

    public override ECameraActionType CameraActionType => ECameraActionType.BlurZoomIn;

    public override Tween Action()
    {
        Camera cameraMain = Camera.main;
        Camera blurCamera = GameObject.Find("BlurAfterCamera").GetComponent<Camera>();

        Sequence seq = DOTween.Sequence();

        seq.Join(cameraMain.transform.DOLocalMove(_targetPos, _zoomInDuration).SetEase(_zoomInEase));
        seq.Join(cameraMain.DOOrthoSize(_targetZoomInSize, _zoomInDuration).SetEase(_zoomInEase));
        seq.Join(blurCamera.DOOrthoSize(_targetZoomInSize, _zoomInDuration).SetEase(_zoomInEase));

        return seq;
    }
}
