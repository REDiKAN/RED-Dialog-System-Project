using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Контейнер для хранения данных диалогового графа
/// </summary>
[Serializable]
public class DialogueContainer : ScriptableObject
{
    [Header("Node Connections")]
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();

    [Header("Dialogue Nodes")]
    public List<DialogueNodeData> DialogueNodeDatas = new List<DialogueNodeData>();

    [Header("Exposed Properties")]
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
}
