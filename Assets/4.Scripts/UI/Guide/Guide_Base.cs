using UnityEngine;

public abstract class Guide_Base<T> : MonoBehaviour where T : class
{
    private RectTransform _rect;
    private Camera _mainCam;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _mainCam = Camera.main;
    }

    public void SetPosition(Transform parent, bool isWorldSpace)
    {
        gameObject.SetActive(true);

        _rect.SetParent(parent, false);

        Vector2 parentScreenPos = RectTransformUtility.WorldToScreenPoint(isWorldSpace ? _mainCam : null, parent.position);

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        bool isRight = parentScreenPos.x >= centerX;
        bool isUp = parentScreenPos.y >= centerY;

        if (isRight && isUp)
        {
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(1f, 1f);
        }
        else if (!isRight && isUp)
        {
            _rect.anchorMin = _rect.anchorMax = new Vector2(1f, 1f);
            _rect.pivot = new Vector2(0f, 1f);
        }
        else if (!isRight && !isUp)
        {
            _rect.anchorMin = _rect.anchorMax = new Vector2(1f, 0f);
            _rect.pivot = new Vector2(0f, 0f);
        }
        else
        {
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 0f);
            _rect.pivot = new Vector2(1f, 0f);
        }
    }

    public abstract void ShowGuide(T t);
}
