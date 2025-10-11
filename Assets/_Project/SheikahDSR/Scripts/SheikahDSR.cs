using Dev.Nicklaj.Butter;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class SheikahDSR : MonoBehaviour
{
    [Foldout("Picture Event")] 
    public GameEvent OnPictureTakenEvent;
    public UnityEvent OnPictureTaken;
    [EndFoldout]

    [Button("Test Picture")]
    public void TakePicture()
    {
        OnPictureTakenEvent.Raise();
        if(OnPictureTakenEvent) OnPictureTaken.Invoke();
    }
}
