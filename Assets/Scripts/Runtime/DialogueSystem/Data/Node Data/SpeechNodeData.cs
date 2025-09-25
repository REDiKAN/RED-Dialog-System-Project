using UnityEngine;
using System;

/// <summary>
/// ������ ���� ���� NPC ��� ������������
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

    public string NodeType;
}
