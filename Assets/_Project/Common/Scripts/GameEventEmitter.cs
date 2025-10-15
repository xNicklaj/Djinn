using Dev.Nicklaj.Butter;
using UnityEngine;

public class GameEventEmitter : MonoBehaviour
{
    public GameEvent Event;
    [Min(0)] public int Channel;

    public void Raise()
    {
        Event.Raise((uint)Channel);
    }
}
