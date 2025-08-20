using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Контейнер для хранения данных диалогового граф
/// </summary>
[Serializable]
public class DialogueContainer : ScriptableObject
{
    [Header("Entry Node")]
    public EntryNodeData EntryNodeData;

    [Header("Node Connections")]
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();

    [Header("Speech Nodes")]
    public List<SpeechNodeData> SpeechNodeDatas = new List<SpeechNodeData>();

    [Header("Option Nodes")]
    public List<OptionNodeData> OptionNodeDatas = new List<OptionNodeData>();

    [Header("Exposed Properties")]
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
}
