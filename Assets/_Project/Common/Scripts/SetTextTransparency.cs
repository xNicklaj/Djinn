using PrimeTween;
using TMPro;
using UnityEngine;
using VInspector;

public class SetTextTransparency : MonoBehaviour
{
    [Min(0)] public float TargetTransparency = 1f;
    public CanvasGroup Target;
    public TweenSettings Settings;

    [Button("Test Set")]
    public void Set()
    {
        Tween.Alpha(Target, TargetTransparency, Settings);
    }
}
