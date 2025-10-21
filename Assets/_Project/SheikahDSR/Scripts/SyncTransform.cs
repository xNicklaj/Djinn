using System;
using Dev.Nicklaj.Butter;
using UnityEngine;

public class SyncTransform : MonoBehaviour
{
    public Vector3Variable Position;
    public QuaternionVariable Rotation;

    private void Update()
    {
        Position.Value = this.transform.position;
        if (!Rotation) return;
        Rotation.Value = this.transform.rotation;
    }
}
