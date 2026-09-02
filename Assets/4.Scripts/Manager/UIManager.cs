using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : SingletonScene<UIManager>
{
    [SerializeField] private SO_HelpData _helpData;

    [SerializeField] private SimplePopup _simplePopup;
    [SerializeField] private Guide_CharacterDetails _guideCharacterDetails;
    [SerializeField] private Guide_Popup _guidePopupPrefab;
    [SerializeField] private Transform _guidePopupParent;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _objBlock;
    [SerializeField] private InvertedMaskTransition _invertedMaskTransition;
    [SerializeField] private SpotlightRevealController _spotlightRevealController;
    [SerializeField] private Image _fadeImage;

    public bool IsLock = false;
    
    private const float GUIDE_POPUP_SPACING = 3f;
    private const float KEYWORD_PREVIEW_HORIZONTAL_OFFSET = 260f;

    private Queue<Guide_Popup> _guidePopupPool = new Queue<Guide_Popup>();
    private Dictionary<Transform, List<Guide_Popup>> _activeGuidePopups = new Dictionary<Transform, List<Guide_Popup>>();
    private Dictionary<EUIType, UI_Base> _dicUIs = new Dictionary<EUIType, UI_Base>();
    private Dictionary<EHelpKey, StHelpData> _dicHelps = new Dictionary<EHelpKey, StHelpData>();
    private KeywordCardPreviewUI _keywordCardPreview;

    public Camera OrthographicCamera { get; private set; }

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();

        OrthographicCamera = GameObject.Find("OrthographicCamera").GetComponent<Camera>();

        _dicHelps.Clear();
        foreach(StHelpData helpData in _helpData.HelpDatas)
        {
            _dicHelps[helpData.HelpKey] = helpData;
        }
    }

    public void SubscribeUI(UI_Base ui)
    {
        if(_dicUIs.ContainsKey(ui.UIType) == false)
        {
            _dicUIs.Add(ui.UIType, ui);
        }
    }

    public void Open(EUIType uiType)
    {
        // Return if the requested UI state does not need changing.
        if (_dicUIs.ContainsKey(uiType) == false)
        {
            return;
        }
        // Return if the requested UI state does not need changing.
        if (_dicUIs[uiType].gameObject.activeSelf == true)
        {
            return;
        }

        _dicUIs[uiType].Open();
    }

    public void Close(EUIType uiType)
    {
        // Return if the requested UI state does not need changing.
        if(_dicUIs.ContainsKey(uiType) == false)
        {
            return;
        }
        // Return if the requested UI state does not need changing.
        if (_dicUIs[uiType].gameObject.activeSelf == false)
        {
            return;
        }

        _dicUIs[uiType].Close();
    }

    public bool HasUI(EUIType uiType)
    {
        return _dicUIs.ContainsKey(uiType);
    }

    public T Get<T>(EUIType uiType) where T : UI_Base
    {
        // Return if the requested UI state does not need changing.
        if (_dicUIs.ContainsKey(uiType) == false)
        {
            return null;
        }

        return _dicUIs[uiType] as T;
    }

    public void ShowGuidePopup(EHelpKey helpKey, Transform tr, bool isWorldSpace = false)
    {
        if(_dicHelps.TryGetValue(helpKey, out StHelpData helpData))
        {
            ShowGuidePopup(LocalizationManager.Instance.Get(helpData.Title), LocalizationManager.Instance.Get(helpData.Contents), tr, isWorldSpace);
        }
    }

    public void ShowGuidePopup(string title, string msg, Transform tr, bool isWorldSpace = false)
    {
        if (!_activeGuidePopups.TryGetValue(tr, out var list))
        {
            list = new List<Guide_Popup>();
            _activeGuidePopups[tr] = list;
        }

        Guide_Popup popup = GetGuidePopupFromPool();
        popup.SetPosition(tr, isWorldSpace);
        popup.SetTitle(title);
        popup.ShowGuide(msg);

        popup.transform.localScale = Vector3.zero;

        list.Add(popup);
        StartCoroutine(CoAnimateGuidePopup(popup, list));
    }

    public void HideGuidePopup(Transform tr, bool isAnimate = true)
    {
        if (_activeGuidePopups.TryGetValue(tr, out var list))
        {
            if(isAnimate)
            {
                foreach (var popup in list)
                {
                    if(popup == null)
                    {
                        continue;
                    }

                    popup.transform.DOKill();
                    ((RectTransform)popup.transform).DOKill();
                    popup.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack)
                        .OnComplete(() => ReturnGuidePopupToPool(popup));
                }
            }
            else
            {
                foreach (var popup in list)
                {
                    if (popup == null)
                    {
                        continue;
                    }

                    ReturnGuidePopupToPool(popup);
                }
            }

            list.Clear();
            _activeGuidePopups.Remove(tr);
        }
    }

    public void HideAllGuidePopup(bool isAnimate = true)
    {
        List<Transform> keys = new List<Transform>();
        foreach(Transform key in _activeGuidePopups.Keys)
        {
            keys.Add(key);
        }

        foreach(Transform key in keys)
        {
            HideGuidePopup(key, isAnimate);
        }
    }

    public void ShowPlayerGuide(Player player, Transform tr, bool worldSpace = false)
    {
        _guideCharacterDetails.SetPosition(tr, worldSpace);
        _guideCharacterDetails.ShowGuide(player);
    }

    public void HideCharacterGuide()
    {
        _guideCharacterDetails.transform.SetParent(transform, false);
        _guideCharacterDetails.gameObject.SetActive(false);
    }

    public void ShowKeywordCardPreview(SO_KeywordData keywordData, Transform anchor)
    {
        if (keywordData == null || anchor == null)
        {
            return;
        }

        KeywordCardPreviewUI preview = GetKeywordCardPreview();
        if (preview == null)
        {
            return;
        }

        if (preview.gameObject.activeSelf)
        {
            preview.HideCardView();
        }

        preview.transform.SetAsLastSibling();
        SetKeywordPreviewPosition(preview.transform as RectTransform, anchor as RectTransform);
        preview.ShowCardView(keywordData, EKeywordCardPreviewType.Guide);
    }

    public void HideKeywordCardPreview()
    {
        if (_keywordCardPreview == null || _keywordCardPreview.gameObject.activeSelf == false)
        {
            return;
        }

        _keywordCardPreview.HideCardView();
    }

    public bool IsCharacterGuideParent(Transform tr)
    {
        return _guideCharacterDetails.transform.parent == tr;
    }
    
    // Shows floating damage text during battle.
    public void ShowDamageText(int damage, Vector3 startPos, Color color)
    {
        DamageTextUI damageTextUI = Creator.Instance.CreatAsset<DamageTextUI>(CreateAssetName.DamageTextUI);
        damageTextUI.transform.SetParent(_canvas.transform);
        damageTextUI.Initialize(damage, startPos, color);
    }

    public void SetActiveSpotlightRevealController(bool flag)
    {
        _spotlightRevealController.gameObject.SetActive(flag);
    }
    public void ClearArtifactPopupQueue()
    {
        foreach (ArtifactPopup popup in _canvas.GetComponentsInChildren<ArtifactPopup>(true))
        {
            Destroy(popup.gameObject);
        }
    }

    public IEnumerator TransitionFadeOut()
    {
        IsLock = true;
        _objBlock.SetActive(true);
        yield return StartCoroutine(_invertedMaskTransition.FadeOut());
    }

    public IEnumerator TransitionFadeIn()
    {
        yield return StartCoroutine(_invertedMaskTransition.FadeIn());
        _objBlock.SetActive(false);
        IsLock = false;
    }

    public float GetFadeDuration()
    {
        return _invertedMaskTransition.AnimationDuration;
    }

    public void ShowSimplePopup(EPopupButtonType poupButtonType, string contents, string leftButtonText = "", string rightButtonText = "", Action onClickLeftButton = null, Action onClickRightButton = null)
    {
        _simplePopup.Open(poupButtonType, contents, leftButtonText, rightButtonText, onClickLeftButton, onClickRightButton);
    }

    public IEnumerator FadeOut(float duration = 0.5f)
    {
        _fadeImage.gameObject.SetActive (true);
        _fadeImage.transform.SetAsLastSibling (); 

        float timer = 0f;
        Color color = _fadeImage.color;
        float startAlpha = color.a;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp (startAlpha, 1f, timer / duration);
            _fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        _fadeImage.color = color;
    }

    public IEnumerator FadeIn(float duration = 0.5f)
    {
        float timer = 0f;
        Color color = _fadeImage.color;
        float startAlpha = color.a;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp (startAlpha, 0f, timer / duration);
            _fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        _fadeImage.color = color;
        _fadeImage.gameObject.SetActive (false);
    }
    
    private IEnumerator CoAnimateGuidePopup(Guide_Popup popup, List<Guide_Popup> list)
    {
        yield return new WaitForEndOfFrame(); // Wait for ContentSizeFitter layout.

        int index = list.IndexOf(popup);

        if (index > 0)
        {
            RectTransform rect = popup.transform as RectTransform;
            float verticalDir = rect.pivot.y >= 0.5f ? -1f : 1f;
            float horizontalDir = rect.pivot.x >= 0.5f ? -1f : 1f;
            float popupWidth = rect.rect.width + GUIDE_POPUP_SPACING;


            RectTransform firstRect = list[0].transform as RectTransform;
            float colHeight = firstRect.rect.height + GUIDE_POPUP_SPACING;
            float colX = 0f;
            Vector2 targetPos = Vector2.zero;

            for (int i = 1; i <= index; i++)
            {
                RectTransform r = list[i].transform as RectTransform;
                Vector2 testPos = new Vector2(colX, colHeight * verticalDir);

                // Move to the next column if this position goes off screen.
                if (IsOffScreen(r, testPos))
                {
                    colX += popupWidth * horizontalDir;
                    colHeight = 0f;
                    testPos = new Vector2(colX, 0f);
                }

                if (i == index)
                {
                    targetPos = testPos;
                    break;
                }

                colHeight += r.rect.height + GUIDE_POPUP_SPACING;
            }

            rect.DOAnchorPos(targetPos, 0.2f).SetEase(Ease.OutCubic);
        }

        popup.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
    }

    private bool IsOffScreen(RectTransform rect, Vector2 testPos)
    {
        Vector2 originalPos = rect.anchoredPosition;
        Vector3 originalScale = rect.localScale;
        
        rect.anchoredPosition = testPos;
        rect.localScale = Vector3.one;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        
        rect.anchoredPosition = originalPos;
        rect.localScale = originalScale;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        foreach (var corner in corners)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, corner);
            if (screenPos.y < 0 || screenPos.y > Screen.height - 130f)
                return true;
        }

        return false;
    }

    // Gets a guide popup from the pool.
    private Guide_Popup GetGuidePopupFromPool()
    {
        if (_guidePopupPool.Count > 0)
        {
            return _guidePopupPool.Dequeue();
        }

        Guide_Popup newPopup = Instantiate(_guidePopupPrefab, _guidePopupParent);
        newPopup.gameObject.SetActive(false);
        return newPopup;
    }

    // Returns a guide popup to the pool.
    private void ReturnGuidePopupToPool(Guide_Popup popup)
    {
        popup.transform.DOKill();
        ((RectTransform)popup.transform).DOKill();
        popup.transform.localScale = Vector3.one;
        popup.transform.SetParent(_guidePopupParent, false);
        ((RectTransform)popup.transform).anchoredPosition = Vector2.zero;
        popup.gameObject.SetActive(false);
        _guidePopupPool.Enqueue(popup);
    }

    private KeywordCardPreviewUI GetKeywordCardPreview()
    {
        if (_keywordCardPreview != null)
        {
            return _keywordCardPreview;
        }

        _keywordCardPreview = Creator.Instance.CreatAsset<KeywordCardPreviewUI>(CreateAssetName.KeywordCardPreview);
        if (_keywordCardPreview == null)
        {
            return null;
        }

        _keywordCardPreview.transform.SetParent(_canvas.transform, false);
        _keywordCardPreview.transform.localScale = Vector3.one;
        _keywordCardPreview.gameObject.SetActive(false);
        return _keywordCardPreview;
    }

    private void SetKeywordPreviewPosition(RectTransform previewRect, RectTransform anchorRect)
    {
        if (previewRect == null || anchorRect == null)
        {
            return;
        }

        RectTransform canvasRect = _canvas.transform as RectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, anchorRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, _canvas.worldCamera, out Vector2 localPoint);

        float direction = localPoint.x <= 0f ? 1f : -1f;
        Vector2 targetPosition = localPoint + new Vector2(KEYWORD_PREVIEW_HORIZONTAL_OFFSET * direction, 0f);

        Vector2 halfCanvasSize = canvasRect.rect.size * 0.5f;
        Vector2 halfPreviewSize = previewRect.rect.size * 0.5f;
        targetPosition.x = Mathf.Clamp(targetPosition.x, -halfCanvasSize.x + halfPreviewSize.x, halfCanvasSize.x - halfPreviewSize.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -halfCanvasSize.y + halfPreviewSize.y, halfCanvasSize.y - halfPreviewSize.y);

        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = targetPosition;
    }
}
