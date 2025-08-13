using System;
using UnityEngine;

/// <summary>
/// Данные узла диалога для сериализации
/// </summary>
[Serializable]
public class DialogueNodeData
{
    [Tooltip("Unique identifier for the node")]
    public string Guid;

    [TextArea, Tooltip("Dialogue text content")]
    public string DialogueText;

    [Tooltip("Position in the graph view")]
    public Vector2 Position;
}
