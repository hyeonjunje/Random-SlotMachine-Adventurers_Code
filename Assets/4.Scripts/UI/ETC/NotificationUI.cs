using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private float _revealTime = 2f;
    [SerializeField] private TMP_Text[] _texts;

    private Queue<TMP_Text> _active = new Queue<TMP_Text>();
    private System.IDisposable _onSendMessageEvent;

    private void OnEnable()
    {
        _onSendMessageEvent = EventBus.Subscribe<StSendMessageEvent>(OnSendMessageEvent);
    }

    private void OnDisable()
    {
        _onSendMessageEvent?.Dispose();
    }

    private void OnSendMessageEvent(StSendMessageEvent sendMessageEvent)
    {
        TMP_Text text = GetText();

        text.text = sendMessageEvent.Message;
        text.alpha = 1f;

        switch (sendMessageEvent.MessageType)
        {
            case EMessageType.Notice:
                text.color = StyleManager.Instance.GetColor(EColorKey.Green);
                break;
            case EMessageType.Warning:
                text.color = StyleManager.Instance.GetColor(EColorKey.Red);
                break;
        }

        DOTween.Kill(text);

        text.DOFade(0f, _revealTime)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (_active.Count > 0 && _active.Peek() == text)
                    _active.Dequeue(); 

                text.gameObject.SetActive(false);
            });
    }

    private TMP_Text GetText()
    {
        foreach (TMP_Text text in _texts)
        {
            if (!text.gameObject.activeSelf)
            {
                _active.Enqueue(text);
                text.gameObject.SetActive(true);
                text.transform.SetAsFirstSibling();
                return text;
            }
        }

        TMP_Text topText = _active.Dequeue();
        DOTween.Kill(topText);
        topText.DOKill();

        topText.alpha = 1f;
        topText.gameObject.SetActive(true);
        topText.transform.SetAsFirstSibling();
        _active.Enqueue(topText);

        return topText;
    }
}
