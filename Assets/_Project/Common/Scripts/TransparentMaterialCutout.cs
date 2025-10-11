using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TransparentMaterialCutout : MonoBehaviour
{
    [Range(0f, 1f)] public float Transparency;
    
    private Material _mat;

    private void Start()
    {
        _mat = GetComponent<MeshRenderer>().material;
    }

    private void SetTransparency()
    {
        
    }
}
