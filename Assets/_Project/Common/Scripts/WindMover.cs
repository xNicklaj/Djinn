using UnityEngine;

public class WindMover : MonoBehaviour
{
    [Tooltip("The object around which the wind moves (e.g., Player, Camera, or Wind Zone).")]
    public Transform centerTransform;

    [Tooltip("How far from the center the wind sound can move in each axis.")]
    public Vector3 range = new Vector3(10f, 5f, 10f);

    [Tooltip("How fast the sound moves toward each target position.")]
    public float moveSpeed = 2f;

    [Tooltip("How long to wait at each target position before picking a new one.")]
    public float waitTime = 2f;

    [Tooltip("Color of gizmos in the scene view.")]
    public Color gizmoColor = new Color(0.4f, 0.7f, 1f, 0.3f);

    private Vector3 targetOffset;
    private float waitTimer;

    void Start()
    {
        if (centerTransform == null)
        {
            Debug.LogWarning("WindMover: No centerTransform assigned. Defaulting to world origin.");
        }
        PickNewTarget();
    }

    void Update()
    {
        Vector3 center = centerTransform ? centerTransform.position : Vector3.zero;
        Vector3 targetPos = center + targetOffset;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                PickNewTarget();
                waitTimer = 0f;
            }
        }
    }

    void PickNewTarget()
    {
        targetOffset = new Vector3(
            Random.Range(-range.x, range.x),
            Random.Range(-range.y, range.y),
            Random.Range(-range.z, range.z)
        );
    }

    // ----------------------------
    // 🧭 Scene View Visualization
    // ----------------------------
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // Draw movement area (centered on the centerTransform if assigned)
        Vector3 center = centerTransform ? centerTransform.position : Vector3.zero;
        Gizmos.DrawWireCube(center, range * 2f);

        // Draw line to current position
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, transform.position);
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Draw target position (in play mode)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 targetPos = center + targetOffset;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }
    }
}
