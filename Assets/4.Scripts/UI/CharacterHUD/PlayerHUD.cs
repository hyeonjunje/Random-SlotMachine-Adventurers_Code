using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private TMP_Text _textLevel;
    [SerializeField] private TMP_Text _textAttackPower;

    [SerializeField] private CharacterHoverUI _hoverUI;

    private Player _player;
    private BoxCollider2D _collider;

    private void Start()
    {
        GetComponent<Canvas>().worldCamera = UIManager.Instance.OrthographicCamera;
    }

    public void Setup(PlayerView playerView, Player player, BoxCollider2D collider)
    {
        _player = player;
        _collider = collider;

        _rect.anchoredPosition = _collider.offset;
        _rect.sizeDelta = _collider.size * 100;

        _player.OnDataChanged += UpdateLevel;
        _player.OnDataChanged += UpdateAttackPower;

        UpdateLevel();
        UpdateAttackPower();

        _hoverUI.SetOwner(playerView);
    }

    public void Release()
    {
        _player.OnDataChanged -= UpdateLevel;
        _player.OnDataChanged -= UpdateAttackPower;
    }

    public void UpdateLevel()
    {
        _textLevel.text = _player.Level.ToString ();
    }

    public void HoverCharacer(bool flag)
    {
        if (flag)
        {
            _hoverUI.OnHoverEnter();
        }
        else
        {
            _hoverUI.OnHoverExit();
        }
    }

    public void UpdateAttackPower()
    {
        _textAttackPower.text = TextParser.Parse("{AttackPower}", _player);
    }
}
