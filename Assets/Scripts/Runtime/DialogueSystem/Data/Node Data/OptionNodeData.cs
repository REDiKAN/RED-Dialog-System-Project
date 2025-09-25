using UnityEngine;
using System;

/// <summary>
/// ������ ���� �������� ������ ������ ��� ������������
/// </summary>
[Serializable]
public class OptionNodeData : BaseNodeData
{
    [TextArea, Tooltip("Response text content")]
    public string ResponseText;

    [Tooltip("GUID of the audio clip asset")]
    public string AudioClipGuid;

    public string NodeType;
}
