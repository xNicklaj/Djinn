using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using UnityEngine;
using VInspector;

public class PathwayManager : MonoBehaviour
{
    private static readonly string LOG_CATEGORY = "Gameplay";
    
    public HiddenDoor[] HiddenDoors;
    public LayerMask IgnoreLayer;

    [Foldout("Camera Transform")] 
    public Vector3Variable CameraPosition;
    public QuaternionVariable CameraRotation;
    [EndFoldout]

    [Button("Find Doors")]
    public void FindDoorsInScene()
    {
        HiddenDoors = FindObjectsByType<HiddenDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    [Button("Test Eval")]
    public void Evaluate()
    {
        var forward = CameraRotation.Value * Vector3.forward;

        foreach (var door in HiddenDoors)
        {
            if (door.State == DoorState.HIDDEN) continue;
            
            Deblog.Log($"Scanning {door.gameObject.name}...", LOG_CATEGORY);
            var distanceVector = (door.transform.position - CameraPosition.Value).normalized;
            var dot = Vector3.Dot(forward, distanceVector);

            // Facing check
            if (dot < 0.7f) continue;
            Deblog.Log($"Dot check for {door.gameObject.name} passed.", LOG_CATEGORY);
            
            // Obstacle check
            var ray = new Ray(CameraPosition.Value, distanceVector);
            if (Physics.Raycast(ray, out var hit, Vector3.Distance(CameraPosition.Value, door.transform.position), ~IgnoreLayer) && hit.transform != door.transform)
            {
                Deblog.Log($"Raycast check failed due to collision with {hit.transform.gameObject.name} at layer {hit.transform.gameObject.layer}.", "Physics");
                continue;
            }
            
            Deblog.Log($"Obstacle check for {door.gameObject.name} passed.", LOG_CATEGORY);
            
            door.Hide();
            
        }
    }
}
