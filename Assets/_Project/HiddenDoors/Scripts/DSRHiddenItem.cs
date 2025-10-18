using System;
using System.Collections;
using dev.nicklaj.clibs.deblog;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

[RequireComponent(typeof(TransparentMaterialCutout))]
public class DSRHiddenItem : MonoBehaviour
{
    private static readonly string LOG_CATEGORY = "Gameplay";
    
    [Tab("Settings")]
    public TweenSettings TweenCurves;
    [Tooltip("Items that will share the same state as this one. When one is hidden the other one is hidden as well.")]
    public DSRHiddenItem[] LinkedItems;
    [Tooltip("Colliders to disable when this door is hidden.")]
    public Collider[] LinkedColliders;
    [Tooltip("Default state of the item.")]
    public DoorState DefaultState = DoorState.HIDDEN;
    [ReadOnly] public DoorState State = DoorState.NOT_SET;
    [EndTab] 
    
    [Tab("Events")] 
    public UnityEvent OnShow;
    public UnityEvent OnHide;
    [EndTab]
    
    private TransparentMaterialCutout _transparentMaterialCutout;

    private void Awake()
    {
        _transparentMaterialCutout = GetComponent<TransparentMaterialCutout>();
    }

    private void Start()
    {
        StartCoroutine(SetDefaultState());
    }

    private IEnumerator SetDefaultState()
    {
        Deblog.Log($"Setting default state for {name} to {DefaultState}", "Gameplay");
        for(var i = 0; i < 5; i++)
            yield return new WaitForEndOfFrame();
        if(DefaultState == DoorState.SHOWN) Show();
        if(DefaultState == DoorState.HIDDEN) Hide();
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
        
        Deblog.Log($"Showing item {gameObject.name}...", LOG_CATEGORY);
        
        foreach(var collider in LinkedColliders) collider.enabled = true;

        Tween.Custom(0f, 1f, TweenCurves, f => _transparentMaterialCutout.Transparency = f);
        State = DoorState.SHOWN;
        OnShow?.Invoke();
        
        foreach(var item in LinkedItems)
            item.Show();
    }

    [Button("Hide")]
    public void Hide()
    {
        if (State == DoorState.HIDDEN) return;
        
        Deblog.Log($"Hiding item {gameObject.name}...", LOG_CATEGORY);
        
        foreach(var collider in LinkedColliders) collider.enabled = false;
            
        Tween.Custom(1f, 0f, TweenCurves, f => _transparentMaterialCutout.Transparency = f);
        State = DoorState.HIDDEN;
        OnHide?.Invoke();
        
        foreach(var item in LinkedItems)
            item.Hide();
    }

    
}

public enum DoorState
{
    SHOWN,
    HIDDEN,
    NOT_SET
}