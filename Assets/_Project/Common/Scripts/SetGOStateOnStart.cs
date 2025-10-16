using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SetGOStateOnStart : MonoBehaviour
{
    public bool Enabled = true;

    private void Start()
    {
        gameObject.SetActive(Enabled);
    }
}
