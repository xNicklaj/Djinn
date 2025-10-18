using System;
using HurricaneVR.Framework.Components;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(HVRPhysicsButton))]
public class FloorTile : MonoBehaviour
{
    public bool YieldsSuccess = false;
    public float DownYOffset = 0.02f;
    
    public bool IsActive 
    {
	    get => _hvr.IsPressed && YieldsSuccess; 
    }
    
    private HVRPhysicsButton _hvr;
    
    private void OnValidate()
    {
	    _hvr = GetComponent<HVRPhysicsButton>();
    }
    
	[Button("Setup HVR Button")]
	private void SetYOffset()
	{
		_hvr.Axis = HVRAxis.Y;
		_hvr.StartPosition = transform.localPosition;
		_hvr.EndPosition = transform.localPosition + Vector3.down * DownYOffset;
	}
}
