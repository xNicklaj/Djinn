using dev.nicklaj.clibs.deblog;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class DSRInteractor : MonoBehaviour
{
    private static readonly string LOG_CATEGORY = "Gameplay";
    
    public bool InteractOnce = true;
    public UnityEvent OnInteract;
    
    public bool CanInteract => !(InteractOnce && _hasInteracted);

    [SerializeField, ReadOnly] private bool _hasInteracted = false;
    
    /// <summary>
    /// Function to call to interact.
    /// </summary>
    [Button("Test Interaction")]
    public void Interact()
    {
        if (InteractOnce && _hasInteracted)
        {
            Deblog.Log($"Skipped interaction with {gameObject.name} since it has already been interacted with.", LOG_CATEGORY);
            return;
        }
        Deblog.Log($"Interacting with {gameObject.name}.", LOG_CATEGORY);
        _hasInteracted = true;
        OnInteract.Invoke();
    }
}
