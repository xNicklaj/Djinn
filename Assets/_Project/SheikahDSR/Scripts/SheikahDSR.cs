using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
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

    [Button("Test Picture")]
    public void TakePicture()
    {
        Deblog.Log("Picture taken.", "Gameplay");
        
        OnPictureTakenEvent.Raise();
        if(OnPictureTakenEvent) OnPictureTaken.Invoke(CameraTexture.ToSprite());
        
        
    }
}
