using UnityEngine;

[DisallowMultipleComponent]
public class FootstepOverride : MonoBehaviour
{
    [Tooltip("Custom footstep sounds for this specific object.")]
    public AudioClip[] overrideFootsteps;

    /// <summary>
    /// Returns a random footstep clip from the override list.
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (overrideFootsteps == null || overrideFootsteps.Length == 0)
            return null;

        int index = Random.Range(0, overrideFootsteps.Length);
        return overrideFootsteps[index];
    }
}