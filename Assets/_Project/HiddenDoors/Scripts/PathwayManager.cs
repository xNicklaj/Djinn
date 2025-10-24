using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using UnityEngine;
using VInspector;

public class PathwayManager : MonoBehaviour
{
    private static readonly string LOG_CATEGORY = "Gameplay";
    
    public DSRInteractor[] DSRInteractors;
    public LayerMask IgnoreLayer;

    [Foldout("Camera Transform")] 
    public Vector3Variable CameraPosition;
    public QuaternionVariable CameraRotation;
    [EndFoldout]

    [Button("Find Interactors")]
    public void FindInteractorsInScene()
    {
        DSRInteractors = FindObjectsByType<DSRInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    [Button("Test Eval")]
    public void Evaluate()
    {
        var forward = CameraRotation.Value * Vector3.forward;

        foreach (var interactor in DSRInteractors)
        {
            if (interactor is null) continue;
            if (!interactor.CanInteract) continue;
            
            Deblog.Log($"Scanning {interactor.gameObject.name}...", LOG_CATEGORY);
            var distanceVector = (interactor.transform.position - CameraPosition.Value).normalized;
            var dot = Vector3.Dot(forward, distanceVector);

            // Facing check
            if (dot < 0.7f) continue;
            Deblog.Log($"Dot check for {interactor.gameObject.name} passed.", LOG_CATEGORY);
            
            // Obstacle check
            var ray = new Ray(CameraPosition.Value, distanceVector);
            if (Physics.Raycast(ray, out var hit, Vector3.Distance(CameraPosition.Value, interactor.transform.position), ~IgnoreLayer) && hit.transform != interactor.transform)
            {
                Deblog.Log($"Raycast check failed due to collision with {hit.transform.gameObject.name} at layer {hit.transform.gameObject.layer}.", "Physics");
                continue;
            }
            
            Deblog.Log($"Obstacle check for {interactor.gameObject.name} passed.", LOG_CATEGORY);
            
            interactor.Interact();
        }
    }
}
