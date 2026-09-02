using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _imageHpBar;
    [SerializeField] private TMP_Text _textHp;
    [SerializeField] private GameObject _objShield;
    [SerializeField] private TMP_Text _textShield;

    private HealthController _healthController;
    private Camera _mainCam;

    public void Init(HealthController healthController)
    {
        if (_healthController != null)
        {
            Unbind (_healthController);
        }

        _mainCam = Camera.main;
        _healthController = healthController;

        if (_slider != null)
        {
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
        }

        if (_healthController == null)
        {
            ShowEmpty ();
            return;
        }

        Bind (_healthController);
        RefreshImmediately ();
    }

    public void Release()
    {
        if (_healthController == null)
        {
            return;
        }

        Unbind (_healthController);
        _healthController = null;
    }

    private void Bind(HealthController healthController)
    {
        healthController.OnChangeHp += SetHpBar;
        healthController.OnChangeShield += SetShield;
        healthController.OnDealDamage += OnDealDamage;
        healthController.OnRestoreHealth += OnRestoreHealth;
    }

    private void Unbind(HealthController healthController)
    {
        healthController.OnChangeHp -= SetHpBar;
        healthController.OnChangeShield -= SetShield;
        healthController.OnDealDamage -= OnDealDamage;
        healthController.OnRestoreHealth -= OnRestoreHealth;
    }

    private void SetHpBar(int hp, int maxHp)
    {
        gameObject.SetActive (true);

        if (maxHp <= 0)
        {
            _slider.normalizedValue = 0f;
            _textHp.text = "0/0";
            return;
        }

        float ratio = Mathf.Clamp01 ((float)hp / maxHp);
        _slider.normalizedValue = ratio;
        _textHp.text = $"{hp}/{maxHp}";
    }

    private void SetShield(int prev, int current)
    {
        _objShield.SetActive (current > 0);
        _textShield.text = current.ToString ();
        _imageHpBar.color = StyleManager.Instance.GetColor (current > 0 ? EColorKey.SkyBlue : EColorKey.Red);
    }

    private void OnDealDamage(int prevHp, int currentHp)
    {
        if (prevHp > currentHp)
        {
            Vector3 screenPos = _mainCam.WorldToScreenPoint (transform.position);
            UIManager.Instance.ShowDamageText (Mathf.Abs (prevHp - currentHp), screenPos, StyleManager.Instance.GetColor (EColorKey.DeBuffSkill));
        }
    }

    private void OnRestoreHealth(int prevHp, int currentHp)
    {
        if (prevHp < currentHp)
        {
            Vector3 screenPos = _mainCam.WorldToScreenPoint (transform.position);
            UIManager.Instance.ShowDamageText (Mathf.Abs (prevHp - currentHp), screenPos, StyleManager.Instance.GetColor (EColorKey.BuffSkill));
        }
    }

    private void RefreshImmediately()
    {
        if (_healthController == null)
        {
            ShowEmpty ();
            return;
        }

        SetHpBar (_healthController.CurrentHp, _healthController.MaxHp);
        SetShield (0, _healthController.Shield);
    }

    private void ShowEmpty()
    {
        gameObject.SetActive (true);
        _slider.normalizedValue = 0f;
        _textHp.text = "0/0";
        SetShield (0, 0);
    }
}
