using System;
using dev.nicklaj.clibs.deblog;
using PrimeTween;
using UnityEngine;
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
    
    private float _heightStep;

    private void OnValidate()
    {
        _heightStep = (StartPosition.y - EndPosition.y) / RequiredSteps;
    }

    public void EvaluatePosition()
    {
        Deblog.Log("Evaluating new cylinder position", "Gameplay");
        var activeTiles = 0;
        for (int i = 0; i < Tiles.Length; i++)
        {
            if(Tiles[i].IsActive) activeTiles++;
            if(activeTiles >= RequiredSteps) break;
        }
        Deblog.Log($"Found {activeTiles} active tiles", "Gameplay");
        
        var _targetPosition = new Vector3(StartPosition.x, StartPosition.y - activeTiles * _heightStep, StartPosition.z);
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
