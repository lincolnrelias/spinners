using System;
using System.Collections.Generic;

public static class EventBus
{
    static readonly Dictionary<string, Action<object>> listeners = new();

    public static void On(string evt, Action<object> cb)
    {
        if (!listeners.ContainsKey(evt)) listeners[evt] = null;
        listeners[evt] += cb;
    }

    public static void Off(string evt, Action<object> cb)
    {
        if (listeners.ContainsKey(evt)) listeners[evt] -= cb;
    }

    public static void Emit(string evt, object data)
    {
        if (listeners.TryGetValue(evt, out var cb)) cb?.Invoke(data);
    }

    public static void Clear() => listeners.Clear();
}

public class CollisionEvent  { public TopBase A, B; public float Force; }
public class DeathEvent      { public TopBase Top; }
public class SpinChangeEvent { public TopBase Top; public float Delta; }
public class BorderHitEvent  { public TopBase Top; }
