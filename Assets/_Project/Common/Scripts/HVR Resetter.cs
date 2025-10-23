using System;
using System.Collections;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using HurricaneVR.Framework.Core;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using VInspector;

public class HVRResetter : MonoBehaviour
{
    [Tab("Configuration")]
    public float MaxY;
    public float MinY;
    [Min(0)] public float MaxLinearVelocity;
    [Min(0)] public float MaxAngularVelocity;
    [Min(0)] public float MaxDistanceFromPlayer;
    public Vector3Variable PlayerPosition;
    [Min(0)] public int FrameExecutionInterval = 10;
    [EndTab] 
    
    [Tab("Grabbables")] 
    public HVRGrabbable[] Grabbables;
    public SerializedTransform[] GrabbablesDefaultState;
    [EndTab] 
    
    
    
    private int _counter = 0;

    [Button("Fill Grabbables")]
    private void FindAllGrabbables()
    {
        if (Application.isPlaying)
        {
            Deblog.LogWarning("Find All Grabbables can only be called outside of play mode.");
            return;
        }
        
        Grabbables = FindObjectsByType<HVRGrabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GrabbablesDefaultState = new SerializedTransform[Grabbables.Length];
        for(var i = 0; i < Grabbables.Length; i++)
            GrabbablesDefaultState[i] = new SerializedTransform(Grabbables[i].transform.position, Grabbables[i].transform.rotation, Grabbables[i].transform.localScale);
    }

    public void Update()
    {
        _counter = (_counter + 1) % FrameExecutionInterval;
        if (_counter > 0) return;
        
        var linearVelocities = new NativeArray<float>(Grabbables.Length, Allocator.TempJob);
        var angularVelocities = new NativeArray<float>(Grabbables.Length, Allocator.TempJob);

        for (var i = 0; i < Grabbables.Length; i++)
        {
            linearVelocities[i] = Grabbables[i].Rigidbody.linearVelocity.magnitude;
            angularVelocities[i] = Grabbables[i].Rigidbody.angularVelocity.magnitude;
        }

        var outputArray = new NativeArray<bool>(Grabbables.Length, Allocator.TempJob);
        var job = new ResetGrabbablesJob()
        {
            MaxY = MaxY,
            MinY = MinY,
            MaxLinearVelocity = MaxLinearVelocity,
            MaxAngularVelocity = MaxAngularVelocity,
            MaxDistance = MaxDistanceFromPlayer,
            PlayerPosition = PlayerPosition.Value,
            LinearVelocity = linearVelocities,
            AngularVelocity = angularVelocities,
            Output = outputArray
        };
        
        var transforms = new Transform[Grabbables.Length];
        for (var i = 0; i < Grabbables.Length; i++)
        {
            transforms[i] = Grabbables[i].transform;
        }
        var transformAccessArray = new TransformAccessArray(transforms);

        var jobHandle = job.ScheduleByRef(transformAccessArray);
        jobHandle.Complete();

        for (var i = 0; i < Grabbables.Length; i++)
        {
            if (!outputArray[i]) continue;

            Grabbables[i].Rigidbody.linearVelocity = Vector3.zero;
            Grabbables[i].Rigidbody.angularVelocity = Vector3.zero;
            
            Grabbables[i].Rigidbody.isKinematic = true;
            Grabbables[i].transform.position = GrabbablesDefaultState[i].position;
            Grabbables[i].transform.rotation = GrabbablesDefaultState[i].rotation;
            Grabbables[i].transform.localScale = GrabbablesDefaultState[i].scale;
            Grabbables[i].Rigidbody.isKinematic = false;    
            
        }
        
        outputArray.Dispose();
        linearVelocities.Dispose();
        angularVelocities.Dispose();
    }
    
    

    [BurstCompile]
    struct ResetGrabbablesJob : IJobParallelForTransform
    {
        [Unity.Collections.ReadOnly]
        public float MaxY;
        [Unity.Collections.ReadOnly]
        public float MinY;

        public NativeArray<float> LinearVelocity;
        public NativeArray<float> AngularVelocity;
        
        [Unity.Collections.ReadOnly]
        public float MaxLinearVelocity;
        [Unity.Collections.ReadOnly]
        public float MaxAngularVelocity;

        [Unity.Collections.ReadOnly] 
        public float MaxDistance;
        [Unity.Collections.ReadOnly] 
        public Vector3 PlayerPosition;

        /// <summary>
        /// True = reset this index.
        /// </summary>
        public NativeArray<bool> Output; 
        
        public void Execute(int index, TransformAccess transform)
        {
            if (transform.position.y < MaxY || transform.position.y > MinY)
            {
                Output[index] = true;
                return;
            }

            if (Vector3.Distance(transform.position, PlayerPosition) > MaxDistance)
            {
                Output[index] = true;
                return;
            }
            
            if(LinearVelocity[index] < MaxLinearVelocity || LinearVelocity[index] > MaxLinearVelocity)
            {
                Output[index] = true;
            }
            
            
        }
    }

    [Serializable]
    public struct SerializedTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public SerializedTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }
}
