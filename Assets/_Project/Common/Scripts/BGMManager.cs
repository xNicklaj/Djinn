using System;
using PrimeTween;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [Range(0f, 1f)]
    public float NormalVolume = 0.3f;
    [field: SerializeField, ReadOnly] public float NormalVolumeDB { get; private set; }
    [Range(0f, 1f)]
    public float LowVolume = 0.1f;
    private AudioSource audioSource;
    
    [field: SerializeField, ReadOnly] public float LowVolumeDB { get; private set; }

    private void OnValidate()
    {
        NormalVolumeDB = 20*Mathf.Log10(NormalVolume);
        LowVolumeDB = 20*Mathf.Log10(LowVolume);
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void ChangeVolume(float endVolume, float interval)
    {
        if (endVolume is < 0 or > 1) return;
        Tween.AudioVolume(audioSource, endVolume, interval, Ease.InOutCubic);
    }

    public void SetLowVolume() => ChangeVolume(LowVolume, .3f);
    public void SetNormalVolume() => ChangeVolume(NormalVolume, .3f);
}
