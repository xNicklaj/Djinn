using dev.nicklaj.clibs.deblog;
using PrimeTween;
using UnityEngine;
using VInspector;

public class ToonTransparency : MonoBehaviour
{
    private static string LOG_CATEGORY = "Rendering";
    private static readonly int TweakTransparency = Shader.PropertyToID("_Tweak_transparency");

    [field: SerializeField, Range(-1f, -.7f)]
    private float _transparency = -.7f;

    public TweenSettings TweenSettings;
    
    private Renderer _meshRenderer;

    private void Awake()
    {
        _meshRenderer = GetComponent<Renderer>();
    }

    public float Transparency
    {
        get => _transparency;
        set
        {
            _transparency = value;
            SetTransparency();
        }
    }

    [OnValueChanged("_transparency")]
    private void SetTransparency()
    {
        if (!Application.isPlaying)
        {
            Deblog.LogError("Material transparency can be changed only when the application is running.", LOG_CATEGORY);
            return;
        }


        for (var i = 0; i < _meshRenderer.materials.Length; i++)
        {
            var mat = _meshRenderer.materials[i];
            
            mat.SetFloat(TweakTransparency, _transparency);
        }
    }

    [Button("Test Hide")]
    public void Hide()
    {
        if (Transparency <= -1f) return;
        Tween.Custom(-.7f, -1f, TweenSettings, f => Transparency = f);
    }
    
    [Button("Test Show")]
    public void Show()
    {
        if (Transparency >= -.7f) return;
        Tween.Custom(-1f, -.7f, TweenSettings, f => Transparency = f);
    }
}
