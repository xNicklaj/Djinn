using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Core.Player;
using UnityEngine;

[RequireComponent(typeof(HVRGlobalInputs))]
public class CalibrateOnPress : MonoBehaviour
{
    private HVRGlobalInputs inputs;
    public HVRCameraRig cameraRig;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputs = GetComponent<HVRGlobalInputs>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!inputs.RightJoystickButtonState.JustActivated) return;
        
        cameraRig.Calibrate();
    }
}
