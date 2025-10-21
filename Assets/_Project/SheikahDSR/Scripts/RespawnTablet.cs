using System;
using Dev.Nicklaj.Butter;
using HurricaneVR.Framework.ControllerInput;
using ImprovedTimers;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(HVRGlobalInputs))]
public class RespawnTablet : MonoBehaviour
{
    [Tab("Configuration")] 
    public GameObject Prefab;
    [Min(0)] public float yOffset;
    public GameEvent DeleteEvent;
    public Vector3Variable LHand;
    [EndTab]
    
    HVRGlobalInputs inputManager;

    private Timer debounceTimer;
    private bool _skipUpdate = false;
    
    private void Awake()
    {
        inputManager = GetComponent<HVRGlobalInputs>();
        debounceTimer = new CountdownTimer(.5f);
        debounceTimer.OnTimerStart += () => _skipUpdate = true;
        debounceTimer.OnTimerStop += () => _skipUpdate = false;
    }

    private void Update()
    {
        if (_skipUpdate) return;
        if (!inputManager.LeftGripButtonState.Active || !inputManager.LeftTriggerButtonState.Active) return;
        
        DeleteEvent.Raise();
        Instantiate(Prefab, LHand.Value + Vector3.up * yOffset, Quaternion.identity);
        debounceTimer.Start();
    }
}
