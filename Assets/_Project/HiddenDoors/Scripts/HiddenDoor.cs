using System;
using dev.nicklaj.clibs.deblog;
using PrimeTween;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(TransparentMaterialCutout))]
public class HiddenDoor : MonoBehaviour
{
    private static readonly string LOG_CATEGORY = "Gameplay";
    
    public TweenSettings TweenCurves;
    [Tooltip("Doors that will share the same state as this one. When one is hidden the other one is hidden as well.")]
    public HiddenDoor[] LinkedDoors;
    [Tooltip("Colliders to disable when this door is hidden.")]
    public Collider[] LinkedColliders;
    [ReadOnly] public DoorState State = DoorState.SHOWN;
    
    
    private TransparentMaterialCutout _transparentMaterialCutout;

    private void Awake()
    {
        _transparentMaterialCutout = GetComponent<TransparentMaterialCutout>();
        Show();
    }
    
    [Button("Attach Colliders")]
    private void FindColliders()
    {
        LinkedColliders = GetComponents<Collider>();
    }

    [Button("Show")]
    public void Show()
    {
        if (State == DoorState.SHOWN) return;
        
        Deblog.Log($"Showing door {gameObject.name}...", LOG_CATEGORY);
        
        foreach(var collider in LinkedColliders) collider.enabled = true;
        
        Tween.Custom(0f, 1f, TweenCurves, f => _transparentMaterialCutout.Transparency = f);
        State = DoorState.SHOWN;
        
        foreach(var door in LinkedDoors)
            door.Show();
    }

    [Button("Hide")]
    public void Hide()
    {
        if (State == DoorState.HIDDEN) return;
        
        Deblog.Log($"Hiding door {gameObject.name}...", LOG_CATEGORY);
        
        foreach(var collider in LinkedColliders) collider.enabled = false;
            
        Tween.Custom(1f, 0f, TweenCurves, f => _transparentMaterialCutout.Transparency = f);
        State = DoorState.HIDDEN;
        
        foreach(var door in LinkedDoors)
            door.Hide();
    }

    
}

public enum DoorState
{
    SHOWN,
    HIDDEN
}