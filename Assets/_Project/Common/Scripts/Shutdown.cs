using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;

public class Shutdown : MonoBehaviour
{
    public Volume Target;
    public TweenSettings Settings;

    public void BlendAndShutdown()
    {
        Tween.Custom(0f, 1f, Settings, f => Target.weight = f)
            .Chain(Tween.Delay(1f))
            .OnComplete(Application.Quit);
    }
}
