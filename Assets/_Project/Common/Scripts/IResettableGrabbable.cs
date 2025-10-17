using Unity.Jobs;
using UnityEngine;

public interface IResettableGrabbable
{
    public struct ShouldReset : IJob
    {
        
        
        public void Execute()
        {
            
        }
    }

    public void Reset();
}
