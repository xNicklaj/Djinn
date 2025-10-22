using System;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using UnityEngine;
using VInspector;

public class CheckInside : MonoBehaviour
{
    [Tab("Config")] 
    [Min(0)] public float RaycastDistance;
    public LayerMask LayerMask;
    [EndTab]
    
    [Tab("References")] 
    public BoolVariable StateVariable; // True for inside, false for ouside
    public GameEvent InsideEvent;
    public GameEvent OutsideEvent;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.up * RaycastDistance);
    }

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.up, out var hit, RaycastDistance, LayerMask))
        {
            if (StateVariable.Value) return;
            
            Deblog.Log("Switching to inside.", "Gameplay");
            StateVariable.Value = true;
            InsideEvent.Raise();
        }
        else
        {
            if (!StateVariable.Value) return;
            
            Deblog.Log("Switched to outside.", "Gameplay");
            StateVariable.Value = false;
            OutsideEvent.Raise();
        }
    }
}
