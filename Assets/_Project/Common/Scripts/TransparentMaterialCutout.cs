using System;
using dev.nicklaj.clibs.deblog;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(MeshRenderer))]
public class TransparentMaterialCutout : MonoBehaviour
{
    private static string LOG_CATEGORY = "Rendering";
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    
    [Range(0f, 1f)] public float Transparency;
    
    private Material _mat;

    private void Start()
    {
        _mat = GetComponent<MeshRenderer>().material;
    }

    [OnValueChanged("Transparency")]
    private void SetTransparency()
    {
        if (!Application.isPlaying)
        {
            Deblog.LogError("Material transparency can be changed only when the application is running.", LOG_CATEGORY);
            return;
        }
        
        var c = _mat.GetColor(BaseColor);
        
        _mat.SetColor(BaseColor, new Color(c.r, c.g, c.b, Transparency));
    }
}
