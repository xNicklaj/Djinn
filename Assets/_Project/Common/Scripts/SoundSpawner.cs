using UnityEngine;
using UnityEngine.Audio;

public class SoundSpawner : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip audioClip;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Range(0f, 1f)] public float volume = 1f;
    public float maxDistance = 50f;
    public AudioMixerGroup outputMixerGroup;

    /// <summary>
    /// Spawns a temporary GameObject that plays the configured sound.
    /// </summary>
    public void Trigger()
    {
        if (audioClip == null)
        {
            Debug.LogWarning("SoundSpawner: No AudioClip assigned!");
            return;
        }

        // Create an empty GameObject (not parented)
        GameObject soundObject = new GameObject($"Audio_{audioClip.name}");
        
        // Add AudioSource and configure
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = audioClip;
        source.spatialBlend = spatialBlend;
        source.volume = volume;
        source.maxDistance = maxDistance;
        source.outputAudioMixerGroup = outputMixerGroup;
        source.Play();

        // Add the auto-destroy component
        AutoDestroyAudio destroyer = soundObject.AddComponent<AutoDestroyAudio>();
        destroyer.audioSource = source;
    }
}