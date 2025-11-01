using System;
using HurricaneVR.Framework.ControllerInput;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VInspector;

public class HMITutorialAnimator : MonoBehaviour
{
    private static readonly int Grip = Animator.StringToHash("Grip");
    private static readonly int StickForward = Animator.StringToHash("StickForward");
    
    [Foldout("Config")]
    public Animator Animator;
    public HMDController Controller;
    public HVRGlobalInputs Inputs;
    [EndFoldout]
    
    [Foldout("Events")]
    public UnityEvent EnableEvent;

    public UnityEvent OnStickEnable;
    public UnityEvent OnStickDisable;
    public UnityEvent OnGripEnable;
    public UnityEvent OnGripDisable;
    [EndFoldout]

    private void OnEnable()
    {
        EnableEvent.Invoke();
    }

    private void Update()
    {
        ListenForStickForward(Controller);
    }

    #region Grip
    [Button("Play Grip")]
    public void PlayGrip() {
        Animator.SetBool(Grip, true);
        OnGripEnable.Invoke();
    }
    [Button("Stop Grip")]
    public void StopGrip()
    {
        Animator.SetBool(Grip, false);
        OnGripDisable.Invoke();
    }
    #endregion
    
    #region StickForward
    [Button("Play StickForward")]
    public void PlayStickForward()
    {
        Animator.SetBool(StickForward, true);
        OnStickEnable.Invoke();
    }
    [Button("Stop StickForward")]
    public void StopStickForward()
    {
        Animator.SetBool(StickForward, false);
        OnStickDisable.Invoke();
    }

    public void ListenForStickForward(HMDController stick)
    {
        if(GetCurrentStickAxis().y > 0) StopStickForward();
    }

    public Vector2 GetCurrentStickAxis()
    {
        return Controller == HMDController.LEFT ? Inputs.LeftJoystickAxis : Inputs.RightJoystickAxis;
    }

    public enum HMDController
    {
        LEFT,
        RIGHT
    }
    #endregion
}
