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

    public FloorTile[] Tiles;
    [Min(1)] public int RequiredSteps;
    public TweenSettings TweenSettings;
    [EndFoldout] 
    
    [Foldout("Events")]
    public UnityEvent OnStart;
    public UnityEvent OnFinish;
    [EndFoldout]
    
    private float _heightStep;

    private void OnValidate()
    {
        _heightStep = (StartPosition.y - EndPosition.y) / RequiredSteps;
    }

    private void Start()
    {
        OnStart.Invoke();
    }

    public void EvaluatePosition()
    {
        Deblog.Log("Evaluating new cylinder position", "Gameplay");
        var activeTiles = 0;
        var enable = false;
        var i = 0;
        for (i = 0; i < Tiles.Length; i++)
        {
            if(Tiles[i].IsActive) activeTiles++;
            if (activeTiles >= RequiredSteps)
            {
                enable = true;
                break;
            }
        }
        Deblog.Log($"Found {activeTiles} active tiles", "Gameplay");
        var endPosition = activeTiles >= RequiredSteps ? StartPosition.y : EndPosition.y;
        var _targetPosition = new Vector3(StartPosition.x, endPosition, StartPosition.z);
        if (activeTiles >= RequiredSteps)
        {
            OnFinish?.Invoke();
        }
        MoveToPosition(_targetPosition);
    }
    

    private void MoveToPosition(Vector3 _targetPosition)
    {
        Tween.PositionY(transform, _targetPosition.y, TweenSettings);
    }
    
    [Button("Set Start Position")]
    private void SetStartPosition() => StartPosition = transform.position;
    
    [Button("Set End Position")]
    private void SetEndPosition() => EndPosition = transform.position;

    [Button("Find All Tiles")]
    private void FindAllTiles()
    {
        Tiles = FindObjectsByType<FloorTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
}
