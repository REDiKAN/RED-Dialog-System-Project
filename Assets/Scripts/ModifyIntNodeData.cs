using UnityEngine;
using System;

[Serializable]
public class ModifyIntNodeData
{
    public string Guid;
    public Vector2 Position;
    public string SelectedProperty;
    public ModifyIntNode.OperatorType Operator;
    public int Value;
}
