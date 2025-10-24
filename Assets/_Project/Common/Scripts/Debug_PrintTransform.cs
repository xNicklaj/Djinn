using System;
using dev.nicklaj.clibs.deblog;
using ImprovedTimers;
using UnityEngine;

public class Debug_PrintTransform : MonoBehaviour
{
    [Min(0)] public float Interval;

    private Timer _timer;

    private void Awake()
    {
        _timer = new CountdownTimer(Interval);
        _timer.OnTimerStop += () =>
        {
            Print();
            _timer.Reset();
            _timer.Start();
        };
    }

    private void OnEnable()
    {
        Print();
        _timer.Start();
    }

    private void Print()
    {
        Deblog.LogWarning($"Transform for {name}: {transform.position} | {transform.rotation}", "Debug");
    }
}
