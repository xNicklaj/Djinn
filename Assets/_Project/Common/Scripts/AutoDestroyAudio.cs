using UnityEngine;

/// <summary>
/// Destroys the GameObject when its AudioSource finishes playing.
/// </summary>
public class AutoDestroyAudio : MonoBehaviour
{
    [HideInInspector] public AudioSource audioSource;

    private void Update()
    {
        if (audioSource == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!audioSource.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}