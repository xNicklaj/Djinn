using System;
using ImprovedTimers;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class MusicRoomManager : MonoBehaviour
{
    public UnityEvent OnPlayed;
    [Min(0)] public float RequiredTime = 3f;
    [ShowInInspector, ReadOnly] private bool IsPlayerInArea = false;

    private Timer _timer;
    private bool _hasTriggered = false;

    private void Awake()
    {
        _timer = new CountdownTimer(RequiredTime);
        _timer.OnTimerStop += () =>
        {
            _hasTriggered = true;
            OnPlayed.Invoke();
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        IsPlayerInArea = true;
    }

    private void OnTriggerExit(Collider other)
    {
        IsPlayerInArea = false;
    }

    public void StartEvaluating()
    {
        if (!IsPlayerInArea) return;
        _timer.Start();
    }

    public void StopEvaluating()
    {
        _timer.Pause();
        _timer.Reset();
    }
}
