using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class FlowNode : Node
{
    /// <summary>���������� ������������� ����</summary>
    public string GUID;

    public FlowNode InputNode;
    public FlowNode NextNode;

    /// <summary>���� ���������� ����</summary>
    public bool EntryPoint;
}

