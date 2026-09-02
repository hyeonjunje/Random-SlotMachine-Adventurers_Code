using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialDialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private RectTransform _rootRect;
    [SerializeField] private TMP_Text _textDialogue;
    [SerializeField] private float _characterInterval = 0.035f;
    [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 110f);

    private Coroutine _typingCoroutine;
    private string _currentText = string.Empty;
    private Action _onCompletedLineConfirmed;
    private bool _isTyping;
    private bool _isConfirmConsumed;
    private Transform _followTarget;

    private Canvas _parentCanvas;

    public bool IsTyping => _isTyping;
    public bool IsShowing => _root != null && _root.activeInHierarchy;

    private void Start()
    {
        _parentCanvas = _rootRect.GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (_root != null && _root.activeInHierarchy && Input.GetMouseButtonDown(0))
        {
            Confirm();
        }
    }

    private void LateUpdate()
    {
        UpdateFollowPosition();
    }

    public void SetFollowTarget(Transform followTarget)
    {
        _followTarget = followTarget;
    }

    public void Show(string text, Action onCompletedLineConfirmed)
    {
        gameObject.SetActive(true);

        ResolveReferences();

        _currentText = text ?? string.Empty;
        _onCompletedLineConfirmed = onCompletedLineConfirmed;
        _isConfirmConsumed = false;

        if (_root == null || _textDialogue == null)
        {
            Debug.LogWarning($"{nameof(TutorialDialogueUI)} needs Root and Text Dialogue references.", this);
            onCompletedLineConfirmed?.Invoke();
            return;
        }

        if (_root != null)
        {
            _root.SetActive(true);
        }

        if (_rootRect == null && _root != null)
        {
            _rootRect = _root.transform as RectTransform;
        }

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        _typingCoroutine = StartCoroutine(CoType());
    }

    public void Hide()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        _isConfirmConsumed = false;
        _onCompletedLineConfirmed = null;

        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    public void Confirm()
    {
        if (IsShowing == false)
        {
            return;
        }

        if (_isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        if (_isConfirmConsumed)
        {
            return;
        }

        _isConfirmConsumed = true;
        Action onCompletedLineConfirmed = _onCompletedLineConfirmed;
        Hide();
        onCompletedLineConfirmed?.Invoke();
    }

    private IEnumerator CoType()
    {
        _isTyping = true;

        if (_textDialogue != null)
        {
            _textDialogue.text = string.Empty;
        }

        for (int i = 0; i < _currentText.Length; i++)
        {
            if (_textDialogue != null)
            {
                _textDialogue.text = _currentText.Substring(0, i + 1);
            }

            yield return new WaitForSecondsRealtime(_characterInterval);
        }

        _isTyping = false;
        _typingCoroutine = null;
    }

    private void CompleteTypingImmediately()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (_textDialogue != null)
        {
            _textDialogue.text = _currentText;
        }

        _isTyping = false;
    }

    private void UpdateFollowPosition()
    {
        if (_followTarget == null || _rootRect == null)
        {
            return;
        }

        Camera worldCamera = UIManager.Instance != null ? UIManager.Instance.OrthographicCamera : Camera.main;
        Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, _followTarget.position);

        Vector3 scaledOffset = (Vector3)_screenOffset * _parentCanvas.scaleFactor;

        _rootRect.position = screenPosition + scaledOffset;
    }

    private void ResolveReferences()
    {
        if (_root == null)
        {
            _root = gameObject;
        }

        if (_rootRect == null && _root != null)
        {
            _rootRect = _root.transform as RectTransform;
        }

        if (_textDialogue == null && _root != null)
        {
            _textDialogue = _root.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
