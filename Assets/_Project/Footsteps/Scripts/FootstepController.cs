using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class FootstepController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Distance the player must travel before playing next footstep sound.")]
    public float stepDistance = 2f;

    [Header("Footstep Sounds")]
    [Tooltip("Footstep sounds for when the player is on terrain.")]
    public AudioClip[] terrainFootsteps;

    [Tooltip("Default mesh footsteps (used when no texture or override match is found).")]
    public AudioClip[] defaultMeshFootsteps;

    [Header("Mesh Texture Footsteps")]
    [Tooltip("List of sounds corresponding to specific textures.")]
    public List<TextureFootstep> textureFootsteps = new List<TextureFootstep>();

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

    [System.Serializable]
    public class TextureFootstep
    {
        [Tooltip("The texture to match on a mesh surface.")]
        public Texture texture;
        [Tooltip("Footstep sounds to play when walking on this texture.")]
        public AudioClip[] footstepSounds;
    }

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
        Vector3 origin = transform.position + Vector3.up * rayOriginOffset;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundLayers))
        {
            // --- 1️⃣ Terrain Check ---
            if (hit.collider.GetComponent<Terrain>() != null)
            {
                return GetRandomClip(terrainFootsteps);
            }

            // --- 2️⃣ FootstepOverride Component Check ---
            FootstepOverride overrideComponent = hit.collider.GetComponent<FootstepOverride>();
            if (overrideComponent != null)
            {
                AudioClip overrideClip = overrideComponent.GetRandomClip();
                if (overrideClip != null)
                    return overrideClip;
            }

            // --- 3️⃣ Texture-based Check ---
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                Texture tex = rend.sharedMaterial.mainTexture;
                if (tex != null)
                {
                    foreach (var entry in textureFootsteps)
                    {
                        if (entry.texture == tex && entry.footstepSounds != null && entry.footstepSounds.Length > 0)
                        {
                            return GetRandomClip(entry.footstepSounds);
                        }
                    }
                }
            }

            // --- 4️⃣ Fallback to Default Mesh Footsteps ---
            return GetRandomClip(defaultMeshFootsteps);
        }

        // --- 5️⃣ No Ground Hit ---
        return GetRandomClip(defaultMeshFootsteps);
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
