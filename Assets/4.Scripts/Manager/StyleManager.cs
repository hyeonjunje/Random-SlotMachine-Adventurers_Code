using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StyleManager : SingletonScene<StyleManager>
{
    [field: SerializeField, Header("애니메이션 시간 데이터")] public SO_AnimationTimeData AnimationTimeData { get; private set; }

    [field: SerializeField, Header("색 데이터")] public SO_ColorPaletteData ColorPaletteData { get; private set; }

    [SerializeField, Header("카메라 액션")] private SO_CameraActionData[] _cameraActions;
    [SerializeField, Header ("키워드 티어에 따른 카드 이미지")] private Sprite[] _keywordTierSprites;
    [SerializeField, Header ("키워드 티어에 따른 색상")] private Color[] _keywordTierColors;

    private Dictionary<ECameraActionType, CameraAction> _dicCameraAcions = new Dictionary<ECameraActionType, CameraAction>();
    private Tween _currentCameraAction = null;

    private Volume _globalVolume;
    private DepthOfField _dof;

    public bool IsEnabledScreenShake { get; set; } = true;

    public Sprite GetKeywordTierSprite(int rank)
    {
        return _keywordTierSprites[rank];
    }
    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();

        _globalVolume = FindAnyObjectByType<Volume>();
        _globalVolume.profile.TryGet(out _dof);

        _dicCameraAcions.Clear();
        foreach(SO_CameraActionData cameraAction in _cameraActions)
        {
            _dicCameraAcions[cameraAction.CameraAction.CameraActionType] = cameraAction.CameraAction;
        }
    }

    public Color GetColor(EColorKey colorKey)
    {
        return ColorPaletteData.GetColor(colorKey);
    }

    public Color GetColor(string colorName)
    {
        return ColorPaletteData.GetColor(colorName);
    }

    public Color GetKeywordTierColor(int rank)
    {
        int index = Mathf.Clamp (rank - 1, 0, _keywordTierColors.Length - 1);
        return _keywordTierColors[index];
    }

    public void SetBlur(bool flag)
    {
        _dof.active = flag;
    }

    public Tween PlayCameraAction(ECameraActionType cameraActionType)
    {
        if(IsEnabledScreenShake == false)
        {
            return null;
        }

        if(_currentCameraAction != null)
        {
            Camera.main.transform.DOComplete();
            _currentCameraAction.Kill();
        }

        if(_dicCameraAcions.TryGetValue(cameraActionType, out CameraAction cameraAction))
        {
            _currentCameraAction = cameraAction.Action();
            _currentCameraAction.Play();

            return _currentCameraAction;
        }
        return null;
    }
}
