using System;
using System.Linq;
using dev.nicklaj.clibs.deblog;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class IJCylinder : MonoBehaviour
{
    [Foldout("Setup")]
    public Vector3 StartPosition;
    public Vector3 EndPosition;
    
    [Min(1)] public int RequiredSteps;
    public TweenSettings TweenSettings;
    [EndFoldout] 
    
    [Foldout("Events")]
    public UnityEvent OnStart;
    public UnityEvent OnFinish;
    [EndFoldout]
    
    private float _heightStep;

    [SerializeField, ReadOnly] private int activeTiles = 0;

    private void OnValidate()
    {
        _heightStep = (StartPosition.y - EndPosition.y) / RequiredSteps;
    }

    private void Start()
    {
        OnStart.Invoke();
    }

    public void EvaluatePosition(bool increment = false)
    {
        Deblog.Log("Evaluating new cylinder position", "Gameplay");
        if(increment) activeTiles++;
        var enable = activeTiles >= RequiredSteps;
        
        Deblog.Log($"Found {activeTiles} active tiles", "Gameplay");
        var endPosition = enable ? EndPosition.y : StartPosition.y;
        var targetPosition = new Vector3(StartPosition.x, endPosition, StartPosition.z);
        if (activeTiles >= RequiredSteps)
        {
            OnFinish?.Invoke();
        }
        MoveToPosition(targetPosition);
    }
    

    private void MoveToPosition(Vector3 _targetPosition)
    {
        Tween.PositionY(transform, _targetPosition.y, TweenSettings);
    }
    
    [Button("Set Start Position")]
    private void SetStartPosition() => StartPosition = transform.position;
    
    [Button("Set End Position")]
    private void SetEndPosition() => EndPosition = transform.position;
}
