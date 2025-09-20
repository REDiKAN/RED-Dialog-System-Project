using UnityEngine;
using System;

/// <summary>
/// Данные узла речи NPC для сериализации
/// </summary>
[Serializable]
public class SpeechNodeData
{
    [Tooltip("Unique identifier for the node")]
    public string Guid;

    [TextArea, Tooltip("Dialogue text content")]
    public string DialogueText;

    [Tooltip("Position in the graph view")]
    public Vector2 Position;

    [Tooltip("GUID of the audio clip asset")]
    public string AudioClipGuid;

    [Tooltip("GUID of the speaker character asset")]
    public string SpeakerGuid;

    public string NodeType;
}
