using DG.Tweening;
using UnityEngine;

public class BlurZoomOutAction : CameraAction
{
    [SerializeField] private float _targetZoomOutSize = 5f;
    [SerializeField] private float _zoomOutDuration = 0.1f;
    [SerializeField] private Ease _zoomOutEase = Ease.OutCubic;

    public override ECameraActionType CameraActionType => ECameraActionType.BlurZoomOut;

    public override Tween Action()
    {
        Camera cameraMain = Camera.main;
        Camera blurCamera = GameObject.Find("BlurAfterCamera").GetComponent<Camera>();

        Sequence seq = DOTween.Sequence();

        seq.Join(cameraMain.transform.DOLocalMove(Vector3.back * 10f, _zoomOutDuration).SetEase(_zoomOutEase));
        seq.Join(cameraMain.DOOrthoSize(_targetZoomOutSize, _zoomOutDuration).SetEase(_zoomOutEase));
        seq.Join(blurCamera.DOOrthoSize(_targetZoomOutSize, _zoomOutDuration).SetEase(_zoomOutEase))
            .Pause();

        return seq;
    }
}
