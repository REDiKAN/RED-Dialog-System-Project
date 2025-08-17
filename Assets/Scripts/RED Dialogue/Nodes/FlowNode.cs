using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class FlowNode : Node
{
    /// <summary>”никальный идентификатор узла</summary>
    public string GUID;

    public FlowNode InputNode;
    public FlowNode NextNode;

    /// <summary>‘лаг стартового узла</summary>
    public bool EntryPoint;
}

