using Dev.Nicklaj.Butter;
using UnityEngine;

public class BoolGameEventEmitter : MonoBehaviour
{
    public BoolEvent Event;
    [Min(0)] public int Channel;

    public void Raise(bool v)
    {
        Event.Raise(v, (uint)Channel);
    }
}
