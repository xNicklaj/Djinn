using System;
using Dev.Nicklaj.Butter;
using UnityEngine;
using UnityEngine.Events;

public class ListenForDestroy : MonoBehaviour
{
    public GameEvent Event;

    private void Awake()
    {
        Event.RegisterListener(Destroy);
    }

    private void Destroy(Unit _)
    {
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        Event.DeregisterListener(Destroy);
    }
}
