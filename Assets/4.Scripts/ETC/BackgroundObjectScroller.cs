using UnityEngine;

public class BackgroundObjectScroller : MonoBehaviour
{
    [Header("Movement Area")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    [Header("Speed Range")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("Vertical Wave Range")]
    [SerializeField] private float minAmplitude = 0f;
    [SerializeField] private float maxAmplitude = 2f;
    [SerializeField] private float minVerticalSpeed = 1f;
    [SerializeField] private float maxVerticalSpeed = 3f;

    private float currentSpeed;
    private float currentAmplitude;
    private float currentVerticalSpeed;
    private bool isMovingRight;
    private float baseYPosition;
    private float verticalTime;

    private void OnEnable()
    {
        InitializeRandomValues(true);
    }

    private void Update()
    {
        MoveObject();
        CheckBounds();
    }

    private void InitializeRandomValues(bool isFirstTime = false)
    {
        isMovingRight = Random.value > 0.5f;

        float startX;
        if (isFirstTime)
        {
            startX = Random.Range(minX, maxX);
        }
        else
        {
            startX = isMovingRight ? minX : maxX;
        }

        float randomY = Random.Range(minY, maxY);
        transform.position = new Vector3(startX, randomY, transform.position.z);

        currentSpeed = Random.Range(minSpeed, maxSpeed);
        currentAmplitude = Random.Range(minAmplitude, maxAmplitude);
        currentVerticalSpeed = Random.Range(minVerticalSpeed, maxVerticalSpeed);

        baseYPosition = randomY;
        verticalTime = Random.Range(0f, Mathf.PI * 2f);
    }

    private void MoveObject()
    {
        float horizontalDirection = isMovingRight ? 1f : -1f;
        float horizontalMovement = horizontalDirection * currentSpeed * Time.deltaTime;

        Vector3 newPosition = transform.position;
        newPosition.x += horizontalMovement;

        if (currentAmplitude > 0f)
        {
            verticalTime += Time.deltaTime * currentVerticalSpeed;
            float verticalOffset = Mathf.Sin(verticalTime) * currentAmplitude;
            newPosition.y = baseYPosition + verticalOffset;
        }

        transform.position = newPosition;
    }

    private void CheckBounds()
    {
        if (isMovingRight && transform.position.x >= maxX)
        {
            InitializeRandomValues();
        }
        else if (!isMovingRight && transform.position.x <= minX)
        {
            InitializeRandomValues();
        }
    }
}
