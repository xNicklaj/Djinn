using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class AudioOcclusion : MonoBehaviour
{
    public Transform listener;             // Usually the player or camera
    public LayerMask occlusionMask;        // Set to your “Wall” layer
    public float normalVolume = 1f;
    public float occludedVolume = 0.3f;
    public float normalCutoff = 22000f;    // Full frequency range
    public float occludedCutoff = 800f;    // Muffled
    public float lerpSpeed = 5f;           // Smooth transition

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private bool isOccluded = false;

    void Start()
    {
        source = GetComponent<AudioSource>();
        lowPass = GetComponent<AudioLowPassFilter>();
    }

    void Update()
    {
        if (listener == null) return;

        // Cast ray from listener to this source
        Vector3 direction = transform.position - listener.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(listener.position, direction, out RaycastHit hit, distance, occlusionMask))
        {
            isOccluded = true;
        }
        else
        {
            isOccluded = false;
        }

        // Smoothly adjust parameters
        float targetVolume = isOccluded ? occludedVolume : normalVolume;
        float targetCutoff = isOccluded ? occludedCutoff : normalCutoff;

        source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * lerpSpeed);
        lowPass.cutoffFrequency = Mathf.Lerp(lowPass.cutoffFrequency, targetCutoff, Time.deltaTime * lerpSpeed);
    }
}