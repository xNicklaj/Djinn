using System;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using HurricaneVR.Framework.Core.Bags;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(HVRSocket))]
public class SpawnIfDistant : MonoBehaviour
{
    [Tab("Config")]
    public Vector3Variable CameraPosition;
    [Min(0)] public float MaxDistance;
    public GameObject Prefab;
    [EndTab] 
    
    [Tab("Events")] 
    public GameEvent DestroyEvent;
    [EndTab] 
    
    private HVRSocket _bag;

    private void Awake()
    {
        _bag =  GetComponent<HVRSocket>();
    }

    private void Update()
    {
        if (!(Vector3.Distance(transform.position, CameraPosition.Value) > MaxDistance)) return;
        
        Deblog.Log("Camera went too far from the player. Respawning in socket.", "Gameplay");
        DestroyEvent.Raise();
        _bag.AutoSpawnPrefab = Prefab;
        _bag.TrySpawnPrefab();
        ResetBag();
    }

    public void ResetBag()
    {
        _bag.AutoSpawnPrefab = null;
    }
}
