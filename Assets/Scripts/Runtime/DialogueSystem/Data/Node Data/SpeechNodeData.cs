using UnityEngine;
using System;

/// <summary>
/// Данные узла речи NPC для сериализации
/// </summary>
[Serializable]
public class SpeechNodeData : BaseNodeData
{
    [TextArea, Tooltip("Dialogue text content")]
    public string DialogueText;

    [Tooltip("GUID of the audio clip asset")]
    public string AudioClipGuid;

    [Tooltip("GUID of the speaker character asset")]
    public string SpeakerGuid;
    public string SpeakerName;

    public string NodeType;
}
