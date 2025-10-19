using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Events;

/// <summary>
/// Утилита для сохранения и загрузки диалоговых графов
/// </summary>
public class GraphSaveUtility
{
    private DialogueGraphView targetGraphView;
    private DialogueContainer containerCache;

    private List<BaseNode> GetNodes() => targetGraphView.nodes.ToList().Cast<BaseNode>().ToList();
    private List<Edge> GetEdges() => targetGraphView.edges.ToList();

    /// <summary>
    /// Получение экземпляра утилиты для сохранения
    /// </summary>
    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
    {
        return new GraphSaveUtility { targetGraphView = targetGraphView };
    }

    #region Saving
    /// <summary>
    /// Сохранение графа в файл
    /// </summary>
    public void SaveGraph(string fileName)
    {
        // Создаем контейнер для данных диалога
        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();

        // Сохраняем узлы и связи
        SaveNodes(dialogueContainer);

        // Сохраняем свойства черной доски
        SaveExposedProperties(dialogueContainer);

        // Создаем папку Resources если не существует
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // Сохраняем ассет
        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", $"Graph saved as {fileName}", "OK");
    }

    /// <summary>
    /// Сохраняет узлы в контейнер
    /// </summary>
    private void SaveNodes(DialogueContainer dialogueContainer)
    {
        dialogueContainer.BaseCharacterGuid = targetGraphView.BaseCharacterGuid;

        // Сохраняем связи между узлами
        var edgesList = targetGraphView.edges.ToList();
        var connectedPorts = edgesList.Where(x => x.input.node != null).ToArray();
        foreach (var edge in connectedPorts)
        {;
            var outputNode = edge.output.node as BaseNode;
            var inputNode = edge.input.node as BaseNode;
            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = edge.output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        // Сохраняем данные узлов
        foreach (var node in GetNodes())
        {
            if (node.EntryPoint)
            {
                dialogueContainer.EntryNodeData = new EntryNodeData
                {
                    Guid = node.GUID,
                    Position = node.GetPosition().position
                };
            }
            else if (node is SpeechNodeText speechNodeText)
            {
                dialogueContainer.SpeechNodeDatas.Add(new SpeechNodeData
                {
                    Guid = speechNodeText.GUID,
                    DialogueText = speechNodeText.DialogueText,
                    Position = node.GetPosition().position,
                    AudioClipGuid = "",
                    SpeakerGuid = speechNodeText.Speaker ? AssetDatabaseHelper.GetAssetGuid(speechNodeText.Speaker) : "",
                    SpeakerName = speechNodeText.Speaker ? speechNodeText.Speaker.name : "",
                    NodeType = "SpeechNodeText"
                });
            }
            else if (node is SpeechNodeAudio speechNodeAudio)
            {
                dialogueContainer.SpeechNodeDatas.Add(new SpeechNodeData
                {
                    Guid = speechNodeAudio.GUID,
                    DialogueText = "", // Аудио-ноды не используют текст
                    Position = node.GetPosition().position,
                    AudioClipGuid = speechNodeAudio.AudioClip ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(speechNodeAudio.AudioClip)) : "",
                    SpeakerGuid = speechNodeAudio.Speaker ? AssetDatabaseHelper.GetAssetGuid(speechNodeAudio.Speaker) : "",
                    NodeType = "SpeechNodeAudio"
                });
            }
            else if (node is SpeechNodeImage speechNodeImage)
            {
                dialogueContainer.SpeechNodeImageDatas.Add(new SpeechNodeImageData
                {
                    Guid = speechNodeImage.GUID,
                    Position = node.GetPosition().position,
                    ImageSpriteGuid = speechNodeImage.ImageSprite ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(speechNodeImage.ImageSprite)) : "",
                    SpeakerGuid = speechNodeImage.Speaker ? AssetDatabaseHelper.GetAssetGuid(speechNodeImage.Speaker) : "",
                    SpeakerName = speechNodeImage.Speaker ? speechNodeImage.Speaker.name : "",
                    NodeType = "SpeechNodeImage"
                });
            }
            else if (node is OptionNodeText optionNodeText)
            {
                dialogueContainer.OptionNodeDatas.Add(new OptionNodeData
                {
                    Guid = optionNodeText.GUID,
                    ResponseText = optionNodeText.ResponseText,
                    Position = node.GetPosition().position,
                    AudioClipGuid = "",
                    NodeType = "OptionNodeText"
                });
            }
            else if (node is OptionNodeAudio optionNodeAudio)
            {
                dialogueContainer.OptionNodeDatas.Add(new OptionNodeData
                {
                    Guid = optionNodeAudio.GUID,
                    ResponseText = "", // Аудио-опции не используют текст
                    Position = node.GetPosition().position,
                    AudioClipGuid = optionNodeAudio.AudioClip ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(optionNodeAudio.AudioClip)) : "",
                    NodeType = "OptionNodeAudio"
                });
            }
            else if (node is OptionNodeImage optionNodeImage)
            {
                dialogueContainer.OptionNodeImageDatas.Add(new OptionNodeImageData
                {
                    Guid = optionNodeImage.GUID,
                    Position = node.GetPosition().position,
                    ImageSpriteGuid = optionNodeImage.ImageSprite ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(optionNodeImage.ImageSprite)) : "",
                    NodeType = "OptionNodeImage"
                });
            }
            else if (node is IntConditionNode intConditionNode)
            {
                dialogueContainer.IntConditionNodeDatas.Add(new IntConditionNodeData
                {
                    Guid = intConditionNode.GUID,
                    Position = node.GetPosition().position,
                    SelectedProperty = intConditionNode.SelectedProperty,
                    Comparison = intConditionNode.Comparison,
                    CompareValue = intConditionNode.CompareValue
                });
            }
            else if (node is StringConditionNode stringConditionNode)
            {
                dialogueContainer.StringConditionNodeDatas.Add(new StringConditionNodeData
                {
                    Guid = stringConditionNode.GUID,
                    Position = node.GetPosition().position,
                    SelectedProperty = stringConditionNode.SelectedProperty,
                    Comparison = (DialogueSystem.StringComparisonType)stringConditionNode.Comparison,
                    CompareValue = stringConditionNode.CompareValue
                });
            }
            else if (node is ModifyIntNode modifyIntNode)
            {
                dialogueContainer.ModifyIntNodeDatas.Add(new ModifyIntNodeData
                {
                    Guid = modifyIntNode.GUID,
                    Position = node.GetPosition().position,
                    SelectedProperty = modifyIntNode.SelectedProperty,
                    Operator = modifyIntNode.Operator,
                    Value = modifyIntNode.Value
                });
            }
            else if (node is EndNode endNode)
            {
                dialogueContainer.EndNodeDatas.Add(new EndNodeData
                {
                    Guid = endNode.GUID,
                    Position = node.GetPosition().position,
                    NextDialogueName = endNode.GetNextDialoguePath()
                });
            }
            else if (node is EventNode eventNode)
            {
                dialogueContainer.EventNodeDatas.Add(new EventNodeData
                {
                    Guid = eventNode.GUID,
                    Position = node.GetPosition().position,
                    Event = eventNode.RuntimeEvent
                });
            }
            else if (node is CharacterIntConditionNode charIntCond)
            {
                dialogueContainer.CharacterIntConditionNodeDatas.Add(new CharacterIntConditionNodeData
                {
                    Guid = charIntCond.GUID,
                    Position = node.GetPosition().position,
                    CharacterName = charIntCond.CharacterName,
                    SelectedVariable = charIntCond.SelectedVariable,
                    Comparison = charIntCond.Comparison,
                    CompareValue = charIntCond.CompareValue
                });
            }
            else if (node is CharacterModifyIntNode charModify)
            {
                dialogueContainer.CharacterModifyIntNodeDatas.Add(new CharacterModifyIntNodeData
                {
                    Guid = charModify.GUID,
                    Position = node.GetPosition().position,
                    CharacterName = charModify.CharacterAsset ? charModify.CharacterAsset.name : "",
                    SelectedVariable = charModify.SelectedVariable,
                    Operator = charModify.Operator,
                    Value = charModify.Value
                });
            }
            else if (node is DebugLogNode debugLog)
            {
                dialogueContainer.DebugLogNodeDatas.Add(new DebugLogNodeData
                {
                    Guid = debugLog.GUID,
                    Position = node.GetPosition().position,
                    MessageText = debugLog.MessageText
                });
            }
            else if (node is DebugWarningNode debugWarn)
            {
                dialogueContainer.DebugWarningNodeDatas.Add(new DebugWarningNodeData
                {
                    Guid = debugWarn.GUID,
                    Position = node.GetPosition().position,
                    MessageText = debugWarn.MessageText
                });
            }
            else if (node is DebugErrorNode debugErr)
            {
                dialogueContainer.DebugErrorNodeDatas.Add(new DebugErrorNodeData
                {
                    Guid = debugErr.GUID,
                    Position = node.GetPosition().position,
                    MessageText = debugErr.MessageText
                });
            }
            else if (node is SpeechNodeRandText speechRandNode)
            {
                dialogueContainer.SpeechRandNodeDatas.Add(new SpeechRandNodeData
                {
                    Guid = speechRandNode.GUID,
                    Position = node.GetPosition().position,
                    SpeakerName = speechRandNode.Speaker ? speechRandNode.Speaker.name : "",
                    Variants = speechRandNode.GetVariants()
                });
            }
            else if (node is RandomBranchNode randomBranchNode)
            {
                dialogueContainer.RandomBranchNodeDatas.Add(new RandomBranchNodeData
                {
                    Guid = randomBranchNode.GUID,
                    Position = node.GetPosition().position,
                    Variants = randomBranchNode.GetVariants()
                });
            }
            else if (node is NoteNode noteNode)
            {
                dialogueContainer.NoteNodeDatas.Add(new NoteNodeData
                {
                    Guid = noteNode.GUID,
                    Position = node.GetPosition().position,
                    NoteText = noteNode.NoteText,
                    BackgroundColor = noteNode.BackgroundColor,
                    ConnectedNodeGuids = noteNode.ConnectedNodeGuids
                });
            }
            else if (node is TimerNode timerNode)
            {
                dialogueContainer.TimerNodeDatas.Add(new TimerNodeData
                {
                    Guid = timerNode.GUID,
                    Position = node.GetPosition().position,
                    DurationSeconds = timerNode.DurationSeconds
                });
            }
            else if (node is PauseNode pauseNode)
            {
                dialogueContainer.PauseNodeDatas.Add(new PauseNodeData
                {
                    Guid = pauseNode.GUID,
                    Position = node.GetPosition().position,
                    DurationSeconds = pauseNode.DurationSeconds
                });
            }

        }
    }

    /// <summary>
    /// Сохранение свойств черной доски
    /// </summary>
    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
        // Очистка старых данных
        dialogueContainer.ExposedProperties.Clear(); // для обратной совместимости, если используется
        dialogueContainer.IntExposedProperties.Clear();
        dialogueContainer.StringExposedProperties.Clear();

        // Сохранение актуальных данных
        dialogueContainer.IntExposedProperties.AddRange(targetGraphView.IntExposedProperties);
        dialogueContainer.StringExposedProperties.AddRange(targetGraphView.StringExposedProperties);
    }
    #endregion

    #region Loading
    /// <summary>
    /// Загрузка графа из файла
    /// </summary>
    public void LoadGraph(string fileName)
    {
        containerCache = Resources.Load<DialogueContainer>(fileName);
        if (containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exist", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
        CreateExposedProperties();

        EditorUtility.DisplayDialog("Success", $"Graph {fileName} loaded", "OK");
    }

    /// <summary>
    /// Создает узлы из загруженных данных
    /// </summary>
    private void CreateNodes()
    {
        // Восстанавливаем EntryNode
        if (containerCache.EntryNodeData != null)
        {
            var entryNode = GetNodes().Find(x => x.EntryPoint);
            if (entryNode != null)
            {
                entryNode.GUID = containerCache.EntryNodeData.Guid;
                entryNode.SetPosition(new Rect(containerCache.EntryNodeData.Position, targetGraphView.DefaultNodeSize));
            }
        }

        // Создаем SpeechNode
        foreach (var nodeData in containerCache.SpeechNodeDatas)
        {
            BaseNode tempNode = null;
            switch (nodeData.NodeType)
            {
                case "SpeechNodeText":
                    tempNode = NodeFactory.CreateSpeechNodeText(nodeData.Position);
                    if (tempNode is SpeechNode speechNodeText)
                    {
                        speechNodeText.SetDialogueText(nodeData.DialogueText);
                    }
                    break;
                case "SpeechNodeAudio":
                    tempNode = NodeFactory.CreateSpeechNodeAudio(nodeData.Position);
                    break;
                default:
                    tempNode = NodeFactory.CreateSpeechNode(nodeData.Position);
                    if (tempNode is SpeechNode speechNode)
                    {
                        speechNode.SetDialogueText(nodeData.DialogueText);
                    }
                    break;
            }
            tempNode.GUID = nodeData.Guid;

            // Восстанавливаем аудио клип по GUID
            if (!string.IsNullOrEmpty(nodeData.AudioClipGuid))
            {
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AssetDatabase.GUIDToAssetPath(nodeData.AudioClipGuid));
                if (tempNode is SpeechNode speechNode)
                {
                    speechNode.AudioClip = audioClip;
                }
            }

            // Восстанавливаем спикера по GUID
            if (!string.IsNullOrEmpty(nodeData.SpeakerGuid))
            {
                var speaker = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(nodeData.SpeakerGuid);
                if (tempNode is SpeechNode speechNode)
                {
                    speechNode.SetSpeaker(speaker);
                }
            }

            targetGraphView.AddElement(tempNode);
        }

        // Создаем SpeechNodeImage
        foreach (var nodeData in containerCache.SpeechNodeImageDatas)
        {
            var tempNode = NodeFactory.CreateSpeechNodeImage(nodeData.Position);
            tempNode.GUID = nodeData.Guid;

            // Восстанавливаем изображение по GUID
            if (!string.IsNullOrEmpty(nodeData.ImageSpriteGuid))
            {
                var imageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(nodeData.ImageSpriteGuid));
                if (tempNode is SpeechNodeImage speechNodeImage)
                {
                    speechNodeImage.ImageSprite = imageSprite;
                }
            }

            // Восстанавливаем спикера по GUID
            if (!string.IsNullOrEmpty(nodeData.SpeakerGuid))
            {
                var speaker = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(nodeData.SpeakerGuid);
                if (tempNode is SpeechNodeImage speechNodeImage)
                {
                    speechNodeImage.SetSpeaker(speaker);
                }
            }

            targetGraphView.AddElement(tempNode);
        }

        // Создаем OptionNode
        foreach (var nodeData in containerCache.OptionNodeDatas)
        {
            BaseNode tempNode = null;
            switch (nodeData.NodeType)
            {
                case "OptionNodeText":
                    tempNode = NodeFactory.CreateOptionNodeText(nodeData.Position);
                    if (tempNode is OptionNode optionNodeText)
                    {
                        optionNodeText.SetResponseText(nodeData.ResponseText);
                    }
                    break;
                case "OptionNodeAudio":
                    tempNode = NodeFactory.CreateOptionNodeAudio(nodeData.Position);
                    break;
                default:
                    tempNode = NodeFactory.CreateOptionNode(nodeData.Position);
                    if (tempNode is OptionNode optionNode)
                    {
                        optionNode.SetResponseText(nodeData.ResponseText);
                    }
                    break;
            }
            tempNode.GUID = nodeData.Guid;

            // Восстанавливаем аудио клип по GUID
            if (!string.IsNullOrEmpty(nodeData.AudioClipGuid))
            {
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AssetDatabase.GUIDToAssetPath(nodeData.AudioClipGuid));
                if (tempNode is OptionNode optionNode)
                {
                    optionNode.AudioClip = audioClip;
                }
            }

            targetGraphView.AddElement(tempNode);
        }

        // Создаем OptionNodeImage
        foreach (var nodeData in containerCache.OptionNodeImageDatas)
        {
            var tempNode = NodeFactory.CreateOptionNodeImage(nodeData.Position);
            tempNode.GUID = nodeData.Guid;

            // Восстанавливаем изображение по GUID
            if (!string.IsNullOrEmpty(nodeData.ImageSpriteGuid))
            {
                var imageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(nodeData.ImageSpriteGuid));
                if (tempNode is OptionNodeImage optionNodeImage)
                {
                    optionNodeImage.ImageSprite = imageSprite;
                }
            }

            targetGraphView.AddElement(tempNode);
        }

        // Создаем IntConditionNode
        foreach (var nodeData in containerCache.IntConditionNodeDatas)
        {
            var tempNode = NodeFactory.CreateIntConditionNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is IntConditionNode intConditionNode)
            {
                intConditionNode.SelectedProperty = nodeData.SelectedProperty;
                intConditionNode.Comparison = nodeData.Comparison;
                intConditionNode.CompareValue = nodeData.CompareValue;
            }
            targetGraphView.AddElement(tempNode);
            // Обновляем UI после добавления в граф
            if (tempNode is IntConditionNode icn)
                icn.UpdateUIFromData();
        }

        // Создаем StringConditionNode
        foreach (var nodeData in containerCache.StringConditionNodeDatas)
        {
            var tempNode = NodeFactory.CreateStringConditionNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is StringConditionNode stringConditionNode)
            {
                stringConditionNode.SelectedProperty = nodeData.SelectedProperty;
                stringConditionNode.Comparison = (StringConditionNode.StringComparisonType)nodeData.Comparison;
                stringConditionNode.CompareValue = nodeData.CompareValue;
            }
            targetGraphView.AddElement(tempNode);
            // Обновляем UI после добавления в граф
            if (tempNode is StringConditionNode scn)
                scn.UpdateUIFromData();
        }

        // Создаем ModifyIntNode
        foreach (var nodeData in containerCache.ModifyIntNodeDatas)
        {
            var tempNode = NodeFactory.CreateModifyIntNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is ModifyIntNode modifyIntNode)
            {
                modifyIntNode.SelectedProperty = nodeData.SelectedProperty;
                modifyIntNode.Operator = nodeData.Operator;
                modifyIntNode.Value = nodeData.Value;
                modifyIntNode.UpdateUIFromData(); // ← добавлено
            }
            targetGraphView.AddElement(tempNode);
        }

        // Создаем EndNode
        foreach (var nodeData in containerCache.EndNodeDatas)
        {
            var tempNode = NodeFactory.CreateEndNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is EndNode endNodeEditor)
            {
                endNodeEditor.SetNextDialogueFromPath(nodeData.NextDialogueName);
            }
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.EventNodeDatas)
        {
            var tempNode = new EventNode();
            tempNode.Initialize(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            tempNode.RuntimeEvent = nodeData.Event;
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.CharacterIntConditionNodeDatas)
        {
            var tempNode = NodeFactory.CreateCharacterIntConditionNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is CharacterIntConditionNode n)
            {
                n.SetInitialData(nodeData.CharacterName, nodeData.SelectedVariable, nodeData.Comparison, nodeData.CompareValue);
                n.UpdateUIFromData();
            }
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.CharacterModifyIntNodeDatas)
        {
            var tempNode = NodeFactory.CreateCharacterModifyIntNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is CharacterModifyIntNode n)
            {
                n.CharacterAsset = CharacterManager.Instance?.GetCharacter(nodeData.CharacterName);
                n.SelectedVariable = nodeData.SelectedVariable;
                n.Operator = nodeData.Operator;
                n.Value = nodeData.Value;
                n.UpdateUIFromData();
            }
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.DebugLogNodeDatas)
        {
            var node = new DebugLogNode();
            node.Initialize(nodeData.Position);
            node.GUID = nodeData.Guid;
            node.MessageText = nodeData.MessageText;
            if (node._previewLabel != null) node._previewLabel.text = nodeData.MessageText;
            targetGraphView.AddElement(node);
        }

        foreach (var nodeData in containerCache.DebugWarningNodeDatas)
        {
            var node = new DebugWarningNode();
            node.Initialize(nodeData.Position);
            node.GUID = nodeData.Guid;
            node.MessageText = nodeData.MessageText;
            if (node._previewLabel != null) node._previewLabel.text = nodeData.MessageText;
            targetGraphView.AddElement(node);
        }

        foreach (var nodeData in containerCache.DebugErrorNodeDatas)
        {
            var node = new DebugErrorNode();
            node.Initialize(nodeData.Position);
            node.GUID = nodeData.Guid;
            node.MessageText = nodeData.MessageText;
            if (node._previewLabel != null) node._previewLabel.text = nodeData.MessageText;
            targetGraphView.AddElement(node);
        }

        foreach (var nodeData in containerCache.SpeechRandNodeDatas)
        {
            var tempNode = NodeFactory.CreateSpeechNodeRandText(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (!string.IsNullOrEmpty(nodeData.SpeakerName))
            {
                var speaker = CharacterManager.Instance?.GetCharacter(nodeData.SpeakerName);
                tempNode.SetSpeaker(speaker);
            }
            tempNode.LoadVariants(nodeData.Variants);
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.RandomBranchNodeDatas)
        {
            var tempNode = NodeFactory.CreateRandomBranchNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is RandomBranchNode randomBranchNode)
            {
                randomBranchNode.LoadVariants(nodeData.Variants);
            }
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.NoteNodeDatas)
        {
            var tempNode = NodeFactory.CreateNoteNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is NoteNode noteNode)
            {
                noteNode.SetNoteText(nodeData.NoteText);
                noteNode.SetBackgroundColor(nodeData.BackgroundColor);
                noteNode.ConnectedNodeGuids = nodeData.ConnectedNodeGuids ?? new List<string>();
            }
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.TimerNodeDatas)
        {
            var tempNode = NodeFactory.CreateTimerNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            tempNode.SetDuration(nodeData.DurationSeconds);
            targetGraphView.AddElement(tempNode);
        }

        foreach (var nodeData in containerCache.TimerNodeDatas)
        {
            // Проверяем, существует ли уже узел с таким GUID
            var existingNode = GetNodes().FirstOrDefault(n => n.GUID == nodeData.Guid);
            BaseNode tempNode;
            if (existingNode != null)
            {
                // Если существует — используем его
                tempNode = existingNode;
                // Обновляем позицию
                tempNode.SetPosition(new Rect(nodeData.Position, targetGraphView.DefaultNodeSize));
            }
            else
            {
                // Если не существует — создаём новый
                tempNode = NodeFactory.CreateTimerNode(nodeData.Position);
                tempNode.GUID = nodeData.Guid;
                targetGraphView.AddElement(tempNode);
            }
            // Обновляем данные узла
            if (tempNode is TimerNode timerNode)
            {
                timerNode.SetDuration(nodeData.DurationSeconds);
            }
        }

        foreach (var nodeData in containerCache.PauseNodeDatas)
        {
            var tempNode = NodeFactory.CreatePauseNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            tempNode.SetDuration(nodeData.DurationSeconds);
            targetGraphView.AddElement(tempNode);
        }
    }

    /// <summary>
    /// Восстановление связей между узлами
    /// </summary>
    /// <summary>
    /// Восстановление связей между узлами
    /// </summary>
    private void ConnectNodes()
    {
        try
        {
            var nodeList = this.targetGraphView.nodes.ToList().Cast<BaseNode>().ToList();
            for (int i = 0; i < nodeList.Count; i++)
            {
                var connections = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeList[i].GUID).ToList();
                foreach (var connection in connections)
                {
                    var targetNodeGuid = connection.TargetNodeGuid;
                    var targetNode = nodeList.FirstOrDefault(x => x.GUID == targetNodeGuid);
                    if (targetNode == null)
                    {
                        Debug.LogWarning($"Target node not found for GUID: {targetNodeGuid}");
                        continue;
                    }

                    Port outputPort = null;

                    // Определяем outputPort в зависимости от типа узла
                    if (nodeList[i] is SpeechNode speechNode)
                    {
                        foreach (var port in speechNode.outputContainer.Children())
                        {
                            if (port is Port portElement && portElement.portName == connection.PortName)
                            {
                                outputPort = portElement;
                                break;
                            }
                        }
                        if (outputPort == null)
                        {
                            outputPort = speechNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                            outputPort.portName = connection.PortName;
                            speechNode.outputContainer.Add(outputPort);
                            speechNode.RefreshPorts();
                            speechNode.RefreshExpandedState();
                        }
                    }
                    else if (nodeList[i] is OptionNode optionNode)
                    {
                        outputPort = optionNode.outputContainer[0].Q<Port>();
                    }
                    else if (nodeList[i] is EntryNode entryNode)
                    {
                        outputPort = entryNode.outputContainer[0].Q<Port>();
                    }
                    else if (nodeList[i] is IntConditionNode || nodeList[i] is StringConditionNode)
                    {
                        foreach (var port in nodeList[i].outputContainer.Children())
                        {
                            if (port is Port portElement && portElement.portName == connection.PortName)
                            {
                                outputPort = portElement;
                                break;
                            }
                        }
                        if (outputPort == null)
                        {
                            Debug.LogWarning($"Port '{connection.PortName}' not found on condition node {nodeList[i].GUID}");
                        }
                    }
                    else if (nodeList[i] is TimerNode timerNode)
                    {
                        foreach (var port in timerNode.outputContainer.Children())
                        {
                            if (port is Port portElement && portElement.portName == connection.PortName)
                            {
                                outputPort = portElement;
                                break;
                            }
                        }
                        if (outputPort == null)
                        {
                            // Восстанавливаем порт, если его нет (например, после загрузки)
                            outputPort = timerNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                            outputPort.portName = connection.PortName;
                            timerNode.outputContainer.Add(outputPort);
                            timerNode.RefreshPorts();
                            timerNode.RefreshExpandedState();
                        }
                    }
                    else if (nodeList[i] is PauseNode pauseNode)
                    {
                        foreach (var port in pauseNode.outputContainer.Children())
                        {
                            if (port is Port portElement && portElement.portName == connection.PortName)
                            {
                                outputPort = portElement;
                                break;
                            }
                        }
                        if (outputPort == null)
                        {
                            // Восстанавливаем порт, если его нет (например, после загрузки)
                            outputPort = pauseNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                            outputPort.portName = connection.PortName;
                            pauseNode.outputContainer.Add(outputPort);
                            pauseNode.RefreshPorts();
                            pauseNode.RefreshExpandedState();
                        }
                    }
                    else if (nodeList[i] is ModifyIntNode modifyNode)
                    {
                        outputPort = modifyNode.outputContainer[0].Q<Port>();
                    }
                    else if (nodeList[i] is EndNode endNode)
                    {
                        outputPort = endNode.outputContainer[0].Q<Port>();
                    }
                    else if (nodeList[i] is EventNode eventNode)
                    {
                        outputPort = eventNode.outputContainer[0].Q<Port>();
                    }

                    // Получаем inputPort у целевого узла
                    Port inputPort = null;
                    if (targetNode.inputContainer.childCount > 0)
                    {
                        inputPort = targetNode.inputContainer[0] as Port;
                    }

                    // Соединяем порты
                    if (outputPort != null && inputPort != null)
                    {
                        LinkNodes(outputPort, inputPort);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to connect {nodeList[i].GUID} -> {targetNodeGuid}. outputPort: {outputPort != null}, inputPort: {inputPort != null}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting nodes: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Связывание двух портов
    /// </summary>
    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge { output = output, input = input };
        tempEdge.input.Connect(tempEdge);
        tempEdge.output.Connect(tempEdge);
        targetGraphView.Add(tempEdge);
    }

    /// <summary>
    /// Восстановление свойств черной доски
    /// </summary>
    private void CreateExposedProperties()
    {
        targetGraphView.ClearBlackBoardAndExposedProperties();

        // Загружаем Int свойства
        foreach (var prop in containerCache.IntExposedProperties)
        {
            var newProp = new IntExposedProperty
            {
                PropertyName = prop.PropertyName,
                IntValue = prop.IntValue,
                MinValue = prop.MinValue,
                MaxValue = prop.MaxValue
            };
            targetGraphView.AddPropertyToBlackBoard(newProp);
        }

        // Загружаем String свойства
        foreach (var prop in containerCache.StringExposedProperties)
        {
            var newProp = new StringExposedProperty
            {
                PropertyName = prop.PropertyName,
                StringValue = prop.StringValue
            };
            targetGraphView.AddPropertyToBlackBoard(newProp);
        }

        // Принудительно обновляем все выпадающие списки после загрузки свойств
        var propertyNodes = targetGraphView.nodes.ToList().OfType<IPropertyNode>();
        foreach (var node in propertyNodes)
            node.RefreshPropertyDropdown();
    }

    /// <summary>
    /// Очистка текущего графа перед загрузкой
    /// </summary>
    private void ClearGraph()
    {
        // Удаляем все узлы кроме стартового
        foreach (var node in GetNodes().Where(node => !node.EntryPoint).ToList())
        {
            // Удаляем связанные связи
            var edgesToRemove = GetEdges().Where(x => x.input.node == node || x.output.node == node).ToList();
            foreach (var edge in edgesToRemove)
            {
                targetGraphView.RemoveElement(edge);
            }

            targetGraphView.RemoveElement(node);
        }
    }
    #endregion

    /// <summary>
    /// Загружает граф из уже загруженного DialogueContainer
    /// </summary>
    public void LoadGraphFromContainer(DialogueContainer container)
    {
        if (container == null)
        {
            Debug.LogError("Cannot load null container");
            return;
        }

        containerCache = container;
        ClearGraph();
        CreateNodes();
        ConnectNodes();
        CreateExposedProperties();
    }

    /// <summary>
    /// Сохраняет граф в существующий DialogueContainer
    /// </summary>
    public void SaveGraphToExistingContainer(DialogueContainer existingContainer)
    {
        if (existingContainer == null)
        {
            Debug.LogError("Cannot save to null container");
            return;
        }

        // === ОЧИЩАЕМ ВСЕ СПИСКИ, ВКЛЮЧАЯ TimerNodeDatas ===
        existingContainer.NodeLinks.Clear();
        existingContainer.SpeechNodeDatas.Clear();
        existingContainer.OptionNodeDatas.Clear();
        existingContainer.IntConditionNodeDatas.Clear();
        existingContainer.StringConditionNodeDatas.Clear();
        existingContainer.ModifyIntNodeDatas.Clear();
        existingContainer.EndNodeDatas.Clear();
        existingContainer.SpeechNodeImageDatas.Clear();
        existingContainer.OptionNodeImageDatas.Clear();
        existingContainer.TimerNodeDatas.Clear(); // ← ДОБАВЛЕНО здесь
        existingContainer.IntExposedProperties.Clear();
        existingContainer.StringExposedProperties.Clear();

        // === Сохраняем новые данные ===
        SaveNodes(existingContainer);
        SaveExposedProperties(existingContainer);

        EditorUtility.SetDirty(existingContainer);
        AssetDatabase.SaveAssets();
    }
}