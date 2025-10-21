using System;
using HurricaneVR.Framework.Core;
using ImprovedTimers;
using UnityEngine;
using VInspector;

public class PlayGuitar : MonoBehaviour
{
    [Tab("Config")] 
    public AudioSource Source;
    public HVRGrabbable Grabbable;
    [Min(0)] public float HisteresisTime = 1f;
    [EndTab] 
    
    private Timer _timer;
    
    private void Awake()
    {
        _timer = new CountdownTimer(HisteresisTime);
        _timer.OnTimerStop += StopSound;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Grabbable.IsHandGrabbed) return;
        if (!other.CompareTag("Player")) return;
        if (_timer.Progress > 0f && _timer.IsRunning)
        {
            _timer.Pause();
            _timer.Reset();
            return;
        }
        
        PlaySound();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _timer.Start();
    }

    public void PlaySound() => Source.Play();
    public void StopSound() => Source.Stop();


}
