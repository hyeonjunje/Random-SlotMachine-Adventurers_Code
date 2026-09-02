using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _textName;
    [SerializeField] private float _fadeDuration = 0.2f;

    [SerializeField] private bool _isPlayer = false;

    private CharacterView _owner;

    private void Start()
    {
        _canvasGroup.alpha = 0;
    }

    private void OnDisable()
    {
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 0;
    }

    public void SetOwner(CharacterView owner)
    {
        _owner = owner;
        _textName.text = owner.Character.GetName();
    }

    public void OnHoverEnter()
    {
        _canvasGroup.DOFade(1f, _fadeDuration);

        if (_isPlayer)
        {
            HashSet<(string, string)> parsedDatas = new HashSet<(string, string)>();

            // 플레이어 파티의 상태이상
            foreach (Status status in CharacterSystem.Instance.PartyStatusController.Statuses.Values)
            {
                string statusRawText = "[" + status.StatusName + "]";
                HashSet<(string, string)> tempParsedDatas = TextParser.ParseBrackets(statusRawText, status.Caster.Character, out string colorizedText);
            
                foreach(var tempParseData in tempParsedDatas)
                {
                    parsedDatas.Add(tempParseData);
                }
            }

            foreach (var parsedData in parsedDatas)
            {
                UIManager.Instance.ShowGuidePopup(parsedData.Item1, parsedData.Item2, transform, true);
            }
        }
        else
        {
            if (_owner == null)
            {
                return;
            }

            Ability ability = _owner.Character.Ability;

            // 적의 Ability
            if (ability != null)
            {
                HashSet<(string name, string explanation)> keywords = TextParser.ParseBrackets(ability.AbilityExplain, _owner.Character, out string parsedDescription);
                UIManager.Instance.ShowGuidePopup(ability.AbilityName, parsedDescription, transform, true);

                foreach (var keyword in keywords)
                {
                    UIManager.Instance.ShowGuidePopup(keyword.name, keyword.explanation, transform, true);
                }

            }

            HashSet<(string, string)> parsedDatas = new HashSet<(string, string)>();

            // 적의 상태이상
            foreach (Status status in _owner.Character.StatusController.Statuses.Values)
            {
                string statusRawText = "[" + status.StatusName + "]";

                Character owner = status.Caster == null ? null : status.Caster.Character;
                HashSet<(string, string)> tempParsedDatas = TextParser.ParseBrackets(statusRawText, owner, out string colorizedText);

                foreach (var tempParseData in tempParsedDatas)
                {
                    parsedDatas.Add(tempParseData);
                }
            }

            foreach (var parsedData in parsedDatas)
            {
                UIManager.Instance.ShowGuidePopup(parsedData.Item1, parsedData.Item2, transform, true);
            }
        }
    }

    public void OnHoverExit()
    {
        _canvasGroup.DOFade(0f, _fadeDuration);

        UIManager.Instance.HideGuidePopup(transform);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }
}
