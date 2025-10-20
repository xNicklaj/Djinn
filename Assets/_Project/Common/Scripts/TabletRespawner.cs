using System.Net.Sockets;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

public class TabletRespawner : MonoBehaviour
{
    public HVRSocket holster;
    public float maxDistance = 2f;
    public float timeRespawner = 1f;

    public bool grabbedFirstTime = false;
    private HVRGrabbable grabbable;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        grabbable = GetComponent<HVRGrabbable>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (grabbable.IsHandGrabbed)
            grabbedFirstTime = true;

        if (grabbedFirstTime)
        {
            float dist = Vector3.Distance(this.transform.position, holster.transform.position);
            if (dist > maxDistance)
            {
                Respawn();
            }
        }
    }

    private void Respawn()
    {
        grabbable.transform.position = holster.transform.position;

    }
}
