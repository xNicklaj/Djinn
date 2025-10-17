using System;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using UnityEngine;
using VInspector;

public class HVRGrabbableDefaultResetter : MonoBehaviour, IResettableGrabbable
{
    [Tab("Object Settings")]
    [Tooltip("Y Position below which the object is reset.")]
    public float MinYPosition;
    [Tooltip("Y Position above which the object is reset.")]
    public float MaxYPosition;
    [EndTab] 
    
    [Tab("Player Relative Settings")]
    [Tooltip("Distance from the player after which the object is reset. Set this to a high value.")]
    public float Distance;
    public Vector3Variable PlayerPosition;
    [EndTab]
    

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < MinYPosition || transform.position.y > MaxYPosition)
        {
            Deblog.LogWarning($"Object {name} hit an invalid Y position. Resetting the object.");
        }   
    }

    
    public bool ShouldReset()
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}
