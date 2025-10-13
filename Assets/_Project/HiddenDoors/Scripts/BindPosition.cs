using System;
using UnityEngine;
using VInspector;

public class BindPosition : MonoBehaviour
{
    [Tab("Target")]
    public Transform Target;
    public BindAxis TargetRotationAxis;
    [EndTab]
    
    [Tab("Height")]
    [Min(0)] public float TargetAngle = 540f;
    public float HeightDifference;
    [EndTab]
    
    private Vector3 _originalPosition;
    
    private float _lastAngle;
    private float _totalAngle;

    private void Start()
    {
        _originalPosition = transform.localPosition;
        _lastAngle = GetEulerAxis(Target.localEulerAngles);
        _totalAngle = 0f;
    }

    private void Update()
    {
        UpdateContinuousRotation();

        if (!(_totalAngle >= 0)) return;
        
        var heightPercent = Mathf.Clamp01(_totalAngle / TargetAngle);
        transform.localPosition = _originalPosition + Vector3.up * (heightPercent * HeightDifference);
    }

    private void UpdateContinuousRotation()
    {
        var currentAngle = GetEulerAxis(Target.localEulerAngles);
        var delta = currentAngle - _lastAngle;

        // Handle wrap-around (360→0 or 0→360)
        if (delta > 180f) delta -= 360f;
        else if (delta < -180f) delta += 360f;

        _totalAngle += delta;
        _totalAngle = Mathf.Max(0f, _totalAngle);
        _lastAngle = currentAngle;
    }

    private float GetEulerAxis(Vector3 euler)
    {
        return TargetRotationAxis switch
        {
            BindAxis.X => euler.x,
            BindAxis.Y => euler.y,
            BindAxis.Z => euler.z,
            _ => throw new NullReferenceException()
        };
    }
}

public enum BindAxis
{
    X,
    Y,
    Z
}