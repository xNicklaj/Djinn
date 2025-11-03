using System;
using dev.nicklaj.clibs.deblog;
using PrimeTween;
using UnityEngine;
using VInspector;

public class ToonTransparency : MonoBehaviour
{
    private static string LOG_CATEGORY = "Rendering";
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int Surface = Shader.PropertyToID("_Surface");

    [field: SerializeField, Range(0f, 1f)]
    private float _transparency = 1f;

    public State InitialState = State.UNSET;
    private State _currentState_internal = State.UNSET;
    public State CurrentState
    {
        get => _currentState_internal;
        set
        {
            if (value == State.SHOWN) Show();
            if (value == State.HIDDEN) Hide();
            _currentState_internal = value;
        }
    }
    public TweenSettings TweenSettings;
    
    private Renderer _meshRenderer;

    private void Awake()
    {
        _meshRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        CurrentState = InitialState;
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
            var color = mat.GetColor(BaseColor);
            mat.SetColor(BaseColor, new Color(color.r, color.g, color.b, Transparency));

            bool isTransparent = !Mathf.Approximately(Transparency, 1f);

            // Core surface toggle
            mat.SetFloat(Surface, isTransparent ? 1f : 0f);
            mat.SetOverrideTag("RenderType", isTransparent ? "Transparent" : "Opaque");
            mat.renderQueue = isTransparent
                ? (int)UnityEngine.Rendering.RenderQueue.Transparent
                : (int)UnityEngine.Rendering.RenderQueue.Geometry;

            // Adjust blending and depth write
            if (isTransparent)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.EnableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            // Force Unity to push updated material state to the renderer
            _meshRenderer.materials[i] = mat;
            _meshRenderer.enabled = Transparency >= 0f;

            // (Optional) If you're using GPU instancing, disable it for this material
            // mat.enableInstancing = false;
        }
    }

    
    private void Hide()
    {
        if (CurrentState == State.HIDDEN) return;
        Tween.Custom(1f, 0f, TweenSettings, f => Transparency = f);
    }
    
    
    private void Show()
    {
        if (CurrentState == State.SHOWN) return;
        Tween.Custom(0f, 1f, TweenSettings, f => Transparency = f);
    }
    
    public void SetState(State state) => CurrentState = state;
    [Button("Test Show")]
    public void SetShown() => SetState(State.SHOWN);
    [Button("Test Hide")]
    public void SetHidden() => SetState(State.HIDDEN);
}

[Serializable]
public enum State
{
    SHOWN,
    HIDDEN,
    UNSET
}
