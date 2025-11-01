#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CollisionLogger : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log($"[CollisionLogger] Initialized on {gameObject.name}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;
        Debug.Log($"[Collision Enter] {gameObject.name} collided with {other.name} at {Time.time:F2}s");

#if UNITY_EDITOR
        // Ping (highlight) the object in the Hierarchy
        EditorGUIUtility.PingObject(other);

        // Select it so it opens in the Inspector
        Selection.activeGameObject = other;
#endif
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log($"[Collision Stay] {gameObject.name} still colliding with {collision.gameObject.name}");
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"[Collision Exit] {gameObject.name} stopped colliding with {collision.gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Trigger Enter] {gameObject.name} entered trigger {other.gameObject.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[Trigger Exit] {gameObject.name} exited trigger {other.gameObject.name}");
    }
}