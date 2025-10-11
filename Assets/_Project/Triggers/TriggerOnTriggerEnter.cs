using System;
using Dev.Nicklaj.Butter;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

[RequireComponent(typeof(BoxCollider))]
public class TriggerOnTriggerEnter : MonoBehaviour
{
    [Tab("Trigger")]
    [Tooltip("Butter event to raise. If null it will not be raised.")]
    public GameEvent GameEvent;
    public UnityEvent Event;
    [EndTab]
    
    [Tab("Layers")]
    public LayerMask IncludeMask = ~0; // defaults to everything
    public LayerMask ExcludeMask;
    [EndTab]
    
    private BoxCollider _collider;

    private void OnEnable()
    {
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsInLayerMask(other.gameObject.layer, IncludeMask) && !IsInLayerMask(other.gameObject.layer, ExcludeMask))
        {
            Trigger();
        }
    }

    [Button("Test Trigger")]
    private void Trigger()
    {
        Event.Invoke();
        if(GameEvent != null)
            GameEvent.Raise();
    }
    
    /// <summary>
    /// Returns true if the specified layer is included in the layer mask.
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return ((layerMask.value & (1 << layer)) != 0);
    }
}
