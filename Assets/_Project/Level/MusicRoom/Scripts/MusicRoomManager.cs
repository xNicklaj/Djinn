using System;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class MusicRoomManager : MonoBehaviour
{
    public UnityEvent OnPlayed;
    [ShowInInspector, ReadOnly] private bool IsPlayerInArea = false;
    
    private void OnTriggerEnter(Collider other)
    {
        IsPlayerInArea = true;
    }

    private void OnTriggerExit(Collider other)
    {
        IsPlayerInArea = false;
    }

    public void Evaluate()
    {
        if (!IsPlayerInArea) return;
        OnPlayed.Invoke();
    }
}
