using System;
using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using VInspector;

public class HMITutorialAnimator : MonoBehaviour
{
    private static readonly int Grip = Animator.StringToHash("Grip");
    private static readonly int Stick = Animator.StringToHash("Stick");
    private static readonly int Secondary = Animator.StringToHash("Stick");
    
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
    public UnityEvent OnSecondaryEnable;
    public UnityEvent OnSecondaryDisable;
    [EndFoldout]

    private void OnEnable()
    {
        EnableEvent.Invoke();
    }

    private void Update()
    {
        ListenForStickAnimation(Controller);
        ListenForGrip(Controller);
        ListenForSecondaryAnimation(Controller);
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
    
    public void ListenForGrip(HMDController stick)
    {
        if (!Animator.GetBool(Grip)) return; 
        if(GetCurrentGripState().JustActivated) StopGrip();
    }

    public HVRButtonState GetCurrentGripState() => Controller == HMDController.LEFT ? Inputs.LeftGripButtonState : Inputs.RightGripButtonState;
    #endregion
    

    #region Stick
    [Button("Play Stick")]
    public void PlayStickAnimation()
    {
        Animator.SetBool(Stick, true);
        OnStickEnable.Invoke();
    }
    [Button("Stop Stick")]
    public void StopStickAnimation()
    {
        Animator.SetBool(Stick, false);
        OnStickDisable.Invoke();
    }

    public void ListenForStickAnimation(HMDController stick)
    {
        if (!Animator.GetBool(Stick)) return; 
        if(GetCurrentStickAxis().magnitude > .4f) StopStickAnimation();
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
    
    #region Secondary
    [Button("Play Secondary")]
    public void PlaySecondaryAnimation()
    {
        Animator.SetBool(Secondary, true);
        OnSecondaryEnable.Invoke();
    }
    [Button("Stop Secondary")]
    public void StopSecondaryAnimation()
    {
        Animator.SetBool(Secondary, false);
        OnSecondaryDisable.Invoke();
    }

    public void ListenForSecondaryAnimation(HMDController stick)
    {
        if (!Animator.GetBool(Secondary)) return;
        if(GetCurrentSecondaryState().JustActivated) StopSecondaryAnimation();
    }
    
    public HVRButtonState GetCurrentSecondaryState() => Controller == HMDController.LEFT ? Inputs.LeftSecondaryButtonState : Inputs.RightSecondaryButtonState;

    #endregion
}
