using System;
using dev.nicklaj.clibs.deblog;
using ImprovedTimers;
using PrimeTween;
using UnityEngine;
using VInspector;

public class SmoothTransformMover : MonoBehaviour
{
    [Foldout("Position")] 
    [Variants("Local Offset", "Local Position", "World Position")]
    public string PositionStrategy;
    public Vector3 Position;
    [EndFoldout] 
    
    public TweenSettings InterpolationSettings;

    private PositionStrategy _positionStrategy;
    
    private void OnValidate()
    {
        _positionStrategy = PositionStrategy switch
        {
            "Local Offset" => global::PositionStrategy.LOCAL_OFFSET,
            "Local Position" => global::PositionStrategy.LOCAL_POSITION,
            "World Position" => global::PositionStrategy.WORLD_POSITION,
            _ => global::PositionStrategy.LOCAL_OFFSET
        };
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        switch (_positionStrategy)
        {
            case global::PositionStrategy.LOCAL_OFFSET:
                Gizmos.DrawWireSphere(transform.localPosition + Position, 0.1f);
                break;
            case global::PositionStrategy.LOCAL_POSITION:
                Gizmos.DrawWireSphere(transform.position + Position, 0.1f);
                break;
            case global::PositionStrategy.WORLD_POSITION:
                Gizmos.DrawWireSphere(Position, 0.1f);
                break;
        }
    }

    public void Move()
    {
        Move(false);
    }

    public void Move(bool ResetOnFinish = false)
    {
        switch (_positionStrategy)
        {
            case global::PositionStrategy.LOCAL_OFFSET:
                Move(transform.localPosition + Position, ResetOnFinish);
                break;
            case global::PositionStrategy.LOCAL_POSITION:
                Move(transform.position + Position, ResetOnFinish);
                break;
            case global::PositionStrategy.WORLD_POSITION:
                Move(Position, ResetOnFinish);
                break;
        }
    }

    public void Move(Vector3 WorldPosition, bool ResetOnFinish = false)
    {
        var initialPosition = transform.position;
        Tween.Position(transform, WorldPosition, InterpolationSettings)
            .OnComplete(() =>
            {
                if (ResetOnFinish)
                {
                    transform.position = initialPosition;
                }
            }) ;
    }

    [Button("Test Movement")]
    public void Test()
    {
        var currPos = transform.position;
        Move(true);
    }
}

public enum PositionStrategy
{
    LOCAL_OFFSET,
    LOCAL_POSITION,
    WORLD_POSITION
}