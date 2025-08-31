using System;
using UnityEngine;

[Serializable]
public class StringConditionNodeData
{
    public string Guid;
    public Vector2 Position;
    public string SelectedProperty;
    public StringConditionNode.StringComparisonType Comparison;
    public string CompareValue;
}
