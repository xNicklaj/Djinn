using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Distance the player must travel before playing next footstep sound.")]
    public float stepDistance = 2f;

    [Header("Footstep Sounds")]
    [Tooltip("Footstep sounds for when the player is on terrain.")]
    public AudioClip[] terrainFootsteps;
    [Tooltip("Footstep sounds for when the player is on a mesh (non-terrain).")]
    public AudioClip[] meshFootsteps;

    [Header("Raycast Settings")]
    [Tooltip("Raycast origin offset from player's position (usually half the height).")]
    public float rayOriginOffset = 1f;
    [Tooltip("How far down the raycast checks for ground.")]
    public float rayDistance = 1.5f;
    [Tooltip("Layers considered as ground.")]
    public LayerMask groundLayers = ~0;

    private AudioSource audioSource;
    private Vector3 lastPosition;
    private float distanceTravelled = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        lastPosition = transform.position;
    }

    void Update()
    {
        TrackDistanceAndPlayFootstep();
    }

    private void TrackDistanceAndPlayFootstep()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        distanceTravelled += distance;

        if (distanceTravelled >= stepDistance)
        {
            PlayFootstepSound();
            distanceTravelled = 0f;
        }

        lastPosition = transform.position;
    }

    private void PlayFootstepSound()
    {
        AudioClip clip = GetFootstepClip();
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private AudioClip GetFootstepClip()
    {
        // Raycast downward to detect what we're standing on
        Vector3 origin = transform.position + Vector3.up * rayOriginOffset;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundLayers))
        {
            if (hit.collider.GetComponent<Terrain>() != null)
            {
                return GetRandomClip(terrainFootsteps);
            }
            else
            {
                return GetRandomClip(meshFootsteps);
            }
        }

        // Default to mesh footsteps if nothing hit
        return GetRandomClip(meshFootsteps);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int index = Random.Range(0, clips.Length);
        return clips[index];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * rayOriginOffset;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);
    }
}
