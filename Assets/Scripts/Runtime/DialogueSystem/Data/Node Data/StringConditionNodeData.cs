using DialogueSystem;
using UnityEngine;
using System;

[Serializable]
public class StringConditionNodeData : BaseNodeData
{
    public string SelectedProperty;
    public StringComparisonType Comparison;
    public string CompareValue;
}
