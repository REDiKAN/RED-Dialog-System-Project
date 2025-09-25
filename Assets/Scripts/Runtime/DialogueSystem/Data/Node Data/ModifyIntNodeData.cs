using DialogueSystem;
using UnityEngine;
using System;

[Serializable]
public class ModifyIntNodeData : BaseNodeData
{
    public string SelectedProperty;
    public OperatorType Operator;
    public int Value;
}
