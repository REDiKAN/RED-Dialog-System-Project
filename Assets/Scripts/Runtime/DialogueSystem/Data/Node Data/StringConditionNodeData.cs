using DialogueSystem;
using UnityEngine;
using System;

[Serializable]
public class StringConditionNodeData
{
    public string Guid;
    public Vector2 Position;
    public string SelectedProperty;
    public StringComparisonType Comparison;
    public string CompareValue;
}
