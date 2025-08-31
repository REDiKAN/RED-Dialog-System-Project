using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// ��������� ��� �������� ������ ����������� ����
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

    [Header("Exposed Properties")]
    public List<IntExposedProperty> IntExposedProperties = new List<IntExposedProperty>();
    public List<StringExposedProperty> StringExposedProperties = new List<StringExposedProperty>();

    [Header("Condition Nodes")]
    public List<IntConditionNodeData> IntConditionNodeDatas = new List<IntConditionNodeData>();
    public List<StringConditionNodeData> StringConditionNodeDatas = new List<StringConditionNodeData>();
}
