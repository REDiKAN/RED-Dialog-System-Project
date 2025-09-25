using UnityEngine;
using System;

/// <summary>
/// Данные узла варианта ответа игрока для сериализации
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
