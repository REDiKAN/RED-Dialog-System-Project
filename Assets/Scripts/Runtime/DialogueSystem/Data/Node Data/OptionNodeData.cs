using UnityEngine;
using System;

/// <summary>
/// ������ ���� �������� ������ ������ ��� ������������
/// </summary>
[Serializable]
public class OptionNodeData
{
    [Tooltip("Unique identifier for the node")]
    public string Guid;

    [TextArea, Tooltip("Response text content")]
    public string ResponseText;

    [Tooltip("Position in the graph view")]
    public Vector2 Position;

    [Tooltip("GUID of the audio clip asset")]
    public string AudioClipGuid;

    public string NodeType;
}
