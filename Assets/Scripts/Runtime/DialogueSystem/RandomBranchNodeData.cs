// Assets/Scripts/Runtime/DialogueSystem/RandomBranchNodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RandomBranchNodeData : BaseNodeData
{
    public List<RandomBranchVariant> Variants = new List<RandomBranchVariant>();
}

[System.Serializable]
public class RandomBranchVariant
{
    public string PortName = "Variant";
    public float WeightPercent = 10f;
}