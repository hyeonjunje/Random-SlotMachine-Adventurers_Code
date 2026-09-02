using DG.Tweening;
using UnityEngine;

public class FloatingBoat : MonoBehaviour
{
    [SerializeField] private float verticalAmount = 0.2f;
    [SerializeField] private float verticalDuration = 1.5f;

    [SerializeField] private float horizontalRange = 0.1f;
    [SerializeField] private float minHorizontalTime = 1.0f;
    [SerializeField] private float maxHorizontalTime = 2.5f;

    private Vector3 _initialShipPosition;

    private void OnEnable()
    {
        _initialShipPosition = transform.localPosition;

        // 배 수직 움직임
        transform.DOLocalMoveY(_initialShipPosition.y + verticalAmount, verticalDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        // 배 수평 움직임
        StartRandomHorizontalMove();
    }

    private void StartRandomHorizontalMove()
    {
        // 무작위 목표 지점과 시간 설정
        float randomX = Random.Range(-horizontalRange, horizontalRange);
        float randomDuration = Random.Range(minHorizontalTime, maxHorizontalTime);

        transform.DOLocalMoveX(_initialShipPosition.x + randomX, randomDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(StartRandomHorizontalMove) // 이동이 끝나면 다시 호출하여 무한 랜덤 반복
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
}
