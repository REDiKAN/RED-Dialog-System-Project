using DialogueSystem;
using UnityEngine;
using System;

[Serializable]
public class CharacterModifyIntNodeData : BaseNodeData
{
    public string CharacterName;
    public string SelectedVariable;
    public OperatorType Operator;
    public int Value;
}