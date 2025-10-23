using UnityEngine;

public class BreakOnImpact : MonoBehaviour
{
    [Header("Break Settings")]
    public GameObject crackedPrefab;   // The broken version
    public float breakForce = 5f;      // Minimum collision magnitude to trigger breaking
    public float explosionForce = 200f;
    public float explosionRadius = 1f;

    private bool hasBroken = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasBroken) return;

        // Determine how hard the impact was
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= breakForce)
        {
            Break(collision);
        }
    }

    void Break(Collision collision)
    {
        hasBroken = true;

        // Spawn cracked prefab
        GameObject cracked = Instantiate(crackedPrefab, transform.position, transform.rotation);

        // Optional: add explosion force to cracked pieces for realism
        Rigidbody[] shards = cracked.GetComponentsInChildren<Rigidbody>();
        Vector3 explosionOrigin = collision.contacts.Length > 0 
            ? collision.contacts[0].point 
            : transform.position;

        foreach (Rigidbody shard in shards)
        {
            shard.AddExplosionForce(explosionForce, explosionOrigin, explosionRadius);
        }

        // Destroy original object
        Destroy(gameObject);
    }
}
