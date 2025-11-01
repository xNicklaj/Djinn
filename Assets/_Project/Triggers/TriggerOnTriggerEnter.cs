using System;
using System.Timers;
using Dev.Nicklaj.Butter;
using dev.nicklaj.clibs.deblog;
using ImprovedTimers;
using UnityEngine;
using UnityEngine.Events;
using VInspector;
using Timer = ImprovedTimers.Timer;

[RequireComponent(typeof(BoxCollider))]
public class TriggerOnTriggerEnter : MonoBehaviour
{
    private static readonly string LOG_CATEGORY = "Physics";

    [Tab("Trigger")] 
    public bool TriggerOnce = false;
    [DisableIf("TriggerOnce")]
    [Min(0)] public float DebouncingTime;
    [EndIf]
    [Tooltip("Butter event to raise. If null it will not be raised.")]
    public GameEvent GameEvent;
    public UnityEvent Event;
    [EndTab]
    
    [Tab("Layers")]
    public LayerMask IncludeMask = ~0; // defaults to everything
    public LayerMask ExcludeMask;
    [EndTab] 
    
    public bool UseDelayOnEnable = true;
    
    private bool IgnoreNext = false;
    private bool _canBeTriggered = true;
    private Timer _timer;
    private BoxCollider _collider;
    private Timer _debouncer;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
        _timer = new CountdownTimer(.3f);
        _debouncer = new CountdownTimer(DebouncingTime);
        _debouncer.OnTimerStart += () => _canBeTriggered = false;
        _debouncer.OnTimerStop += () => _canBeTriggered = true;
    }

    private void OnEnable()
    {
        if (!UseDelayOnEnable) return;
        _timer.OnTimerStart += () => IgnoreNext = true;
        _timer.OnTimerStop += () => IgnoreNext = false;
        _timer.Start();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Collider>().isTrigger) return;
        if (IgnoreNext)
        {
            Deblog.Log($"Ignoring collision with {other.gameObject.name}");
            return;
        }
        
        Deblog.Log($"Trigger {gameObject.name} collided with {other.gameObject.name}", LOG_CATEGORY);
        if (IsInLayerMask(other.gameObject.layer, IncludeMask) && !IsInLayerMask(other.gameObject.layer, ExcludeMask))
        {
            Trigger();
            if (TriggerOnce) _collider.enabled = false;
        }
    }

    [Button("Test Trigger")]
    private void Trigger()
    {
        if (!_canBeTriggered)
        {
            Deblog.Log($"Avoiding Trigger due to debouncing timer. {DebouncingTime - _debouncer.CurrentTime} seconds left.");
            return;
        }
        Event.Invoke();
        Deblog.Log($"Collision accepted. Triggering interaction with {gameObject.name}.", LOG_CATEGORY);
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
