using System;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using HurricaneVR.Framework.Core;
using ImprovedTimers;
using UnityEngine;
using VInspector;

public class PlayMizmar : MonoBehaviour
{
    [Tab("Config")]
    public Vector3Variable CameraPosition;
    public float MaxDistanceFromCamera = 2f;
    public AudioSource Source;
    public HVRGrabbable Grabbable;
    [Min(0)] public float HisteresisTime = 1f;
    public GameEvent OnPlayed;
    [EndTab] 
    
    private Timer _timer;
    
    private void Awake()
    {
        _timer = new CountdownTimer(HisteresisTime);
        _timer.OnTimerStop += StopSound;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Grabbable.IsHandGrabbed)
        {
            Deblog.Log("Mizmar not played because it is not being held.", "Gameplay");
            return;
        };
        if (!other.CompareTag("Player")) return;
        if (Vector3.Distance(CameraPosition.Value, transform.position) > MaxDistanceFromCamera)
        {
            Deblog.Log($"Mizmar not played because it is too distant from the player camera. Current Distance: {Vector3.Distance(CameraPosition.Value, transform.position)}, Max Allowed: {MaxDistanceFromCamera}.");
            return;
        };
        
        if (_timer.Progress > 0f && _timer.IsRunning)
        {
            _timer.Pause();
            _timer.Reset();
            return;
        }
        
        OnPlayed.Raise();
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
