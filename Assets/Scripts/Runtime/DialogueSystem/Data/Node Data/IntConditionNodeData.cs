using DialogueSystem;
using UnityEngine;
using System;

[Serializable]
public class IntConditionNodeData
{
    public string Guid;
    public Vector2 Position;
    public string SelectedProperty;
    public ComparisonType Comparison;
    public int CompareValue;
}
