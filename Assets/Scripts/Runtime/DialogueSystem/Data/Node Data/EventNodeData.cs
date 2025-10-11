using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public class EventNodeData : BaseNodeData
{
    public UnityEvent Event = new UnityEvent();
}