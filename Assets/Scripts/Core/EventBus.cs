using System;
using System.Collections.Generic;

public interface IEvent { }

public class EventBus
{
    private readonly Dictionary<Type, List<WeakReference<Action<IEvent>>>> _subscribers
        = new Dictionary<Type, List<WeakReference<Action<IEvent>>>>();

    public void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var type = typeof(T);

        if (!_subscribers.ContainsKey(type))
            _subscribers[type] = new List<WeakReference<Action<IEvent>>>();

        Action<IEvent> wrapper = (e) => handler((T)e);

        _subscribers[type].Add(new WeakReference<Action<IEvent>>(wrapper));
    }

    public void Publish<T>(T evt) where T : IEvent
    {
        var type = typeof(T);

        if (!_subscribers.TryGetValue(type, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].TryGetTarget(out var handler))
                handler(evt);
            else
                list.RemoveAt(i);
        }
    }
}
