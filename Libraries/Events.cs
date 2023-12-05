using System;
using System.Collections.Generic;
using System.Reflection;

namespace DojoArenaKT;

public class Event
{
    public static Dictionary<Events, List<MethodInfo>> Subscribers = new();
    public static void Fire(Events eventID, object[] arguments = null)
    {

        if (!Subscribers.ContainsKey(eventID)) return;
        foreach (var subscriber in Subscribers[eventID])
        {
            try
            {
                subscriber.Invoke(null, arguments);
            }
            catch (Exception ex)
            {
                Output.LogError($"FireEvent-{Enum.GetName(eventID)}-{subscriber.Name}", ex);
            }
        }
    }

    public static void RegisterEvents()
    {
        if (!G.MethodsWithAttributes.ContainsKey(typeof(EventSubscriberAttribute))) return;

        var methodList = G.MethodsWithAttributes[typeof(EventSubscriberAttribute)];
        methodList.Sort((tupl1, tupl2) => {
            var attribute1 = (EventSubscriberAttribute)tupl1.Attribute;
            var attribute2 = (EventSubscriberAttribute)tupl2.Attribute;
            return attribute1.Priority.CompareTo(attribute2.Priority);
        });

        foreach ((MethodInfo method, Attribute genericAttribute) in methodList)
        {
            EventSubscriberAttribute attrib = (EventSubscriberAttribute)genericAttribute;
            Events subscription = attrib.SubscribedEvent;
            if (!Subscribers.ContainsKey(subscription)) Subscribers.Add(subscription, new());
            Subscribers[subscription].Add(method);
        }
    }
}

public enum Events
{
    Preload,                // Main.cs
    Load,                   // Libraries/Timer.cs
    Unload,
    BootstrapStart,
    BootstrapQuit,          // Modules/Bootstrap.cs
}
public class EventSubscriberAttribute : System.Attribute
{
    public Events SubscribedEvent;
    public int Priority;
    public EventSubscriberAttribute(Events subscribedEvent, int priority=0)
    {
        SubscribedEvent = subscribedEvent;
        Priority = priority;
    }
}