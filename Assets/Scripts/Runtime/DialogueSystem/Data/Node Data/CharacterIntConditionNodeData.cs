using DialogueSystem;
using UnityEngine;
using System;

[Serializable]
public class CharacterIntConditionNodeData : BaseNodeData
{
    public string CharacterName;
    public string SelectedVariable;
    public ComparisonType Comparison;
    public int CompareValue;
}