using TMPro;
using UnityEngine;

public class EnemyHUD : MonoBehaviour
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private HpBar _hpBar;
    [SerializeField] private StatusView _statusView;
    [SerializeField] private TMP_Text _textActCount;
    [SerializeField] private GameObject _objTarget;
    [SerializeField] private TMP_Text _targetOrder;
    [SerializeField] private TMP_Text _textAttackPower;

    [SerializeField] private CharacterHoverUI _hoverUI;

    private Enemy _enemy;
    private BoxCollider2D _collider;

    public void Setup(EnemyView enemyView, Enemy enemy, HealthController healthContoller, StatusController statusController, BoxCollider2D collider)
    {
        _enemy = enemy;
        _collider = collider;

        _rect.anchoredPosition = _collider.offset;
        _rect.sizeDelta = _collider.size * 100;

        _hpBar.Init(healthContoller);
        _statusView.Init(statusController);

        _enemy.EnemyAI.OnChangeActCount += SetActCount;
        _enemy.OnDataChanged += UpdateAttackPower;

        UpdateAttackPower();

        _hoverUI.SetOwner(enemyView);
    }

    public void Release()
    {
        _hpBar.Release();
        _statusView.Release();

        _enemy.EnemyAI.OnChangeActCount -= SetActCount;
        _enemy.OnDataChanged -= UpdateAttackPower;
    }

    public void SetActCount(int count, bool isAct)
    {
        if(isAct)
        {
            _textActCount.text = "X";
        }
        else
        {
            _textActCount.text = count.ToString();
        }
    }

    public void SetActiveTarget(bool flag, int order)
    {
        _objTarget.SetActive(flag);
        _targetOrder.gameObject.SetActive(flag);
        _targetOrder.text = order.ToString();
    }

    public void HoverCharacer(bool flag)
    {
        if(flag)
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
        _textAttackPower.text = TextParser.Parse("{AttackPower}", _enemy);
    }
}
