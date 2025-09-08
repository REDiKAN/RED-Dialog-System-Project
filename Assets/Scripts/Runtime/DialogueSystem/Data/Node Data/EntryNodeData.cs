using UnityEngine;
using System;

/// <summary>
/// ƒанные стартового узла дл€ сериализации
/// </summary>
[Serializable]
public class EntryNodeData
{
    [Tooltip("Unique identifier for the node")]
    public string Guid;

    [Tooltip("Position in the graph view")]
    public Vector2 Position;
}
