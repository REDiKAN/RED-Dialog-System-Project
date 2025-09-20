using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// ������� ��� ���������� � �������� ���������� ������
/// </summary>
public class GraphSaveUtility
{
    private DialogueGraphView targetGraphView;
    private DialogueContainer containerCache;
    private List<Edge> Edges => targetGraphView.edges.ToList();
    private List<BaseNode> Nodes => targetGraphView.nodes.ToList().Cast<BaseNode>().ToList();

    /// <summary>
    /// ��������� ���������� ������� ��� ����������
    /// </summary>
    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
    {
        return new GraphSaveUtility { targetGraphView = targetGraphView };
    }

    #region Saving
    /// <summary>
    /// ���������� ����� � ����
    /// </summary>
    public void SaveGraph(string fileName)
    {
        // ������� ��������� ��� ������ �������
        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();

        // ��������� ���� � �����
        SaveNodes(dialogueContainer);

        // ��������� �������� ������ �����
        SaveExposedProperties(dialogueContainer);

        // ������� ����� Resources ���� �� ����������
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // ��������� �����
        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", $"Graph saved as {fileName}", "OK");
    }

    /// <summary>
    /// ��������� ���� � ���������
    /// </summary>
    private void SaveNodes(DialogueContainer dialogueContainer)
    {
        dialogueContainer.BaseCharacterGuid = targetGraphView.BaseCharacterGuid;

        // ��������� ����� ����� ������
        var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        foreach (var edge in connectedPorts)
        {
            var outputNode = edge.output.node as BaseNode;
            var inputNode = edge.input.node as BaseNode;

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = edge.output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        // ��������� ������ �����
        foreach (var node in Nodes)
        {
            if (node.EntryPoint)
            {
                // ��������� ������ EntryNode
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
                    NodeType = "SpeechNodeText"
                });
            }
            else if (node is SpeechNodeAudio speechNodeAudio)
            {
                dialogueContainer.SpeechNodeDatas.Add(new SpeechNodeData
                {
                    Guid = speechNodeAudio.GUID,
                    DialogueText = "",
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
                    ResponseText = "",
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
                    NextDialogueName = endNode.NextDialogueName
                });
            }
        }
    }

    /// <summary>
    /// ���������� ������� ������ �����
    /// </summary>
    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
        dialogueContainer.ExposedProperties.Clear();
        dialogueContainer.ExposedProperties.AddRange(targetGraphView.ExposedProperties);
    }
    #endregion

    #region Loading
    /// <summary>
    /// �������� ����� �� �����
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
    /// ������� ���� �� ����������� ������
    /// </summary>
    private void CreateNodes()
    {
        // ��������������� EntryNode
        if (containerCache.EntryNodeData != null)
        {
            var entryNode = Nodes.Find(x => x.EntryPoint);
            if (entryNode != null)
            {
                entryNode.GUID = containerCache.EntryNodeData.Guid;
                entryNode.SetPosition(new Rect(containerCache.EntryNodeData.Position, targetGraphView.DefaultNodeSize));
            }
        }

        // ������� SpeechNode
        foreach (var nodeData in containerCache.SpeechNodeDatas)
        {
            BaseNode tempNode = null;

            switch (nodeData.NodeType)
            {
                case "SpeechNodeText":
                    tempNode = NodeFactory.CreateSpeechNodeText(nodeData.Position, nodeData.DialogueText);
                    break;
                case "SpeechNodeAudio":
                    tempNode = NodeFactory.CreateSpeechNodeAudio(nodeData.Position);
                    break;
                default:
                    tempNode = NodeFactory.CreateSpeechNode(nodeData.Position, nodeData.DialogueText);
                    break;
            }

            tempNode.GUID = nodeData.Guid;

            // ��������������� ����� ���� �� GUID
            if (!string.IsNullOrEmpty(nodeData.AudioClipGuid))
            {
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AssetDatabase.GUIDToAssetPath(nodeData.AudioClipGuid));
                if (tempNode is SpeechNode speechNode)
                {
                    speechNode.AudioClip = audioClip;
                }
            }

            // ��������������� ������� �� GUID
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

        // ������� SpeechNodeImage
        foreach (var nodeData in containerCache.SpeechNodeImageDatas)
        {
            var tempNode = NodeFactory.CreateSpeechNodeImage(nodeData.Position);
            tempNode.GUID = nodeData.Guid;

            // ��������������� ����������� �� GUID
            if (!string.IsNullOrEmpty(nodeData.ImageSpriteGuid))
            {
                var imageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(nodeData.ImageSpriteGuid));
                if (tempNode is SpeechNodeImage speechNodeImage)
                {
                    speechNodeImage.ImageSprite = imageSprite;
                }
            }

            // ��������������� ������� �� GUID
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

        // ������� OptionNode
        foreach (var nodeData in containerCache.OptionNodeDatas)
        {
            BaseNode tempNode = null;

            switch (nodeData.NodeType)
            {
                case "OptionNodeText":
                    tempNode = NodeFactory.CreateOptionNodeText(nodeData.Position, nodeData.ResponseText);
                    break;
                case "OptionNodeAudio":
                    tempNode = NodeFactory.CreateOptionNodeAudio(nodeData.Position);
                    break;
                default:
                    tempNode = NodeFactory.CreateOptionNode(nodeData.Position, nodeData.ResponseText);
                    break;
            }

            tempNode.GUID = nodeData.Guid;

            // ��������������� ����� ���� �� GUID
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

        // ������� OptionNodeImage
        foreach (var nodeData in containerCache.OptionNodeImageDatas)
        {
            var tempNode = NodeFactory.CreateOptionNodeImage(nodeData.Position);
            tempNode.GUID = nodeData.Guid;

            // ��������������� ����������� �� GUID
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

        // ������� IntConditionNode
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
        }

        // ������� StringConditionNode
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
        }

        // ������� ModifyIntNode
        foreach (var nodeData in containerCache.ModifyIntNodeDatas)
        {
            var tempNode = NodeFactory.CreateModifyIntNode(nodeData.Position);
            tempNode.GUID = nodeData.Guid;
            if (tempNode is ModifyIntNode modifyIntNode)
            {
                modifyIntNode.SelectedProperty = nodeData.SelectedProperty;
                modifyIntNode.Operator = nodeData.Operator;
                modifyIntNode.Value = nodeData.Value;
            }
            targetGraphView.AddElement(tempNode);
        }

        // ������� EndNode
        foreach (var nodeData in containerCache.EndNodeDatas)
        {
            var tempNode = NodeFactory.CreateEndNode(nodeData.Position, nodeData.NextDialogueName);
            tempNode.GUID = nodeData.Guid;
            targetGraphView.AddElement(tempNode);
        }
    }

    /// <summary>
    /// �������������� ������ ����� ������
    /// </summary>
    private void ConnectNodes()
    {
        try
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var connections = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();

                foreach (var connection in connections)
                {
                    var targetNodeGuid = connection.TargetNodeGuid;
                    var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);

                    // ������� ��������������� ����
                    Port outputPort = null;

                    if (Nodes[i] is SpeechNode speechNode)
                    {
                        // ���� ���� � ������ ������
                        foreach (var port in speechNode.outputContainer.Children())
                        {
                            if (port is Port portElement && portElement.portName == connection.PortName)
                            {
                                outputPort = portElement;
                                break;
                            }
                        }

                        // ���� ���� �� ������, ������� ���
                        if (outputPort == null)
                        {
                            outputPort = speechNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
                            outputPort.portName = connection.PortName;
                            speechNode.outputContainer.Add(outputPort);
                            speechNode.RefreshPorts();
                            speechNode.RefreshExpandedState();
                        }
                    }
                    else if (Nodes[i] is OptionNode optionNode)
                    {
                        outputPort = optionNode.outputContainer[0].Q<Port>();
                    }
                    else if (Nodes[i] is EntryNode entryNode)
                    {
                        outputPort = entryNode.outputContainer[0].Q<Port>();
                    }

                    var inputPort = (Port)targetNode.inputContainer[0];

                    if (outputPort != null && inputPort != null)
                    {
                        LinkNodes(outputPort, inputPort);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting nodes: {e.Message}");
        }
    }

    /// <summary>
    /// ���������� ���� ������
    /// </summary>
    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge { output = output, input = input };
        tempEdge.input.Connect(tempEdge);
        tempEdge.output.Connect(tempEdge);
        targetGraphView.Add(tempEdge);
    }

    /// <summary>
    /// �������������� ������� ������ �����
    /// </summary>
    private void CreateExposedProperties()
    {
        targetGraphView.ClearBlackBoardAndExposedProperties();
        foreach (var exposedProperty in containerCache.ExposedProperties)
        {
            targetGraphView.AddPropertyToBlackBoard(exposedProperty);
        }
    }

    /// <summary>
    /// ������� �������� ����� ����� ���������
    /// </summary>
    private void ClearGraph()
    {
        // ������� ��� ���� ����� ����������
        foreach (var node in Nodes.Where(node => !node.EntryPoint).ToList())
        {
            // ������� ��������� �����
            var edgesToRemove = Edges.Where(x => x.input.node == node || x.output.node == node).ToList();
            foreach (var edge in edgesToRemove)
            {
                targetGraphView.RemoveElement(edge);
            }

            targetGraphView.RemoveElement(node);
        }
    }
    #endregion
}