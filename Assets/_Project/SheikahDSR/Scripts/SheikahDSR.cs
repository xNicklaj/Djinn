using System;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using ImprovedTimers;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class SheikahDSR : MonoBehaviour
{
    public RenderTexture CameraTexture;
    
    [Foldout("Picture Event")] 
    public GameEvent OnPictureTakenEvent;
    public UnityEvent<Sprite> OnPictureTaken;
    [EndFoldout] 
    
    private Timer _debouncer;
    private bool _canTakePicture = true;

    private void Awake()
    {
        _debouncer = new CountdownTimer(.5f);
        _debouncer.OnTimerStart += () => _canTakePicture = false;
        _debouncer.OnTimerStop += () => _canTakePicture = true;
    }

    [Button("Test Picture")]
    public void TakePicture()
    {
        if (!_canTakePicture) return;
        Deblog.Log("Picture taken.", "Gameplay");
        
        _debouncer.Start();
        OnPictureTakenEvent.Raise();
        if(OnPictureTakenEvent) OnPictureTaken.Invoke(CameraTexture.ToSprite());
    }
}
