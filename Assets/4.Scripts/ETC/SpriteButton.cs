using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class SpriteButton : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Events")]
    public UnityEvent onClick;

    private SpriteRenderer spriteRenderer;
    private bool isPressed = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (normalSprite == null && spriteRenderer.sprite != null)
            normalSprite = spriteRenderer.sprite; // 현재 스프라이트를 기본값으로
    }

    void OnMouseEnter()
    {
        if (!isPressed && hoverSprite != null)
            spriteRenderer.sprite = hoverSprite;
    }

    void OnMouseExit()
    {
        if (!isPressed && normalSprite != null)
            spriteRenderer.sprite = normalSprite;
    }

    void OnMouseDown()
    {
        isPressed = true;
        if (pressedSprite != null)
            spriteRenderer.sprite = pressedSprite;
    }

    void OnMouseUp()
    {
        isPressed = false;
        if(normalSprite != null)
            spriteRenderer.sprite = normalSprite;
        onClick?.Invoke();
    }
}
