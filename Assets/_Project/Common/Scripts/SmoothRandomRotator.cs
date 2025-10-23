using UnityEngine;

public class SmoothRandomRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float minSpeed = 10f;
    public float maxSpeed = 60f;
    public float minChangeInterval = 1f;
    public float maxChangeInterval = 4f;
    public float smoothness = 2f; // higher = smoother (slower transition)

    private Vector3 currentAxis;
    private Vector3 targetAxis;
    private float currentSpeed;
    private float targetSpeed;
    private float timer;
    private float changeInterval;

    void Start()
    {
        PickNewRotation();
        currentAxis = targetAxis;
        currentSpeed = targetSpeed;
    }

    void Update()
    {
        // Smoothly blend toward the new rotation axis & speed
        currentAxis = Vector3.Lerp(currentAxis, targetAxis, Time.deltaTime * smoothness).normalized;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * smoothness);

        // Apply rotation
        transform.Rotate(currentAxis * currentSpeed * Time.deltaTime, Space.Self);

        // Check if it's time to pick a new direction
        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            PickNewRotation();
            timer = 0f;
        }
    }

    void PickNewRotation()
    {
        // Random new axis
        targetAxis = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        // Random new speed and interval
        targetSpeed = Random.Range(minSpeed, maxSpeed);
        changeInterval = Random.Range(minChangeInterval, maxChangeInterval);
    }
}
