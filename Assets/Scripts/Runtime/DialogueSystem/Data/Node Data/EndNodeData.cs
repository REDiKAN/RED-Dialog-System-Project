using System;

/// <summary>
/// Данные конечного узла
/// </summary>
[Serializable]
public class EndNodeData : BaseNodeData
{
    public string NextDialogueName;
    public bool ShouldEndDialogue = true;
}