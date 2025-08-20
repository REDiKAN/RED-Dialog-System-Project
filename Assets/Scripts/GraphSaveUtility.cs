using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Утилита для сохранения и загрузки диалоговых графов
/// </summary>
public class GraphSaveUtility
{
    private DialogueGraphView targetGraphView;
    private DialogueContainer containerCache;
    private List<Edge> Edges => targetGraphView.edges.ToList();
    private List<BaseNode> Nodes => targetGraphView.nodes.ToList().Cast<BaseNode>().ToList();

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
    /// Сохранение узлов и связей
    /// </summary>
    private void SaveNodes(DialogueContainer dialogueContainer)
    {
        // Сохраняем связи между узлами
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

        // Сохраняем данные узлов
        foreach (var node in Nodes)
        {
            if (node.EntryPoint)
            {
                // Сохраняем данные EntryNode
                dialogueContainer.EntryNodeData = new EntryNodeData
                {
                    Guid = node.GUID,
                    Position = node.GetPosition().position
                };
            }
            else if (node is SpeechNode speechNode)
            {
                dialogueContainer.SpeechNodeDatas.Add(new SpeechNodeData
                {
                    Guid = speechNode.GUID,
                    DialogueText = speechNode.DialogueText,
                    Position = node.GetPosition().position,
                    AudioClipGuid = speechNode.AudioClip ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(speechNode.AudioClip)) : ""
                });
            }
            else if (node is OptionNode optionNode)
            {
                dialogueContainer.OptionNodeDatas.Add(new OptionNodeData
                {
                    Guid = optionNode.GUID,
                    ResponseText = optionNode.ResponseText,
                    Position = node.GetPosition().position,
                    AudioClipGuid = optionNode.AudioClip ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(optionNode.AudioClip)) : ""
                });
            }
        }
    }

    /// <summary>
    /// Сохранение свойств черной доски
    /// </summary>
    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
        dialogueContainer.ExposedProperties.Clear();
        dialogueContainer.ExposedProperties.AddRange(targetGraphView.ExposedProperties);
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
    /// Создание узлов из загруженных данных
    /// </summary>
    private void CreateNodes()
    {
        // Восстанавливаем EntryNode
        if (containerCache.EntryNodeData != null)
        {
            var entryNode = Nodes.Find(x => x.EntryPoint);
            if (entryNode != null)
            {
                entryNode.GUID = containerCache.EntryNodeData.Guid;
                entryNode.SetPosition(new Rect(containerCache.EntryNodeData.Position, targetGraphView.DefaultNodeSize));
            }
        }

        // Создаем SpeechNode
        foreach (var nodeData in containerCache.SpeechNodeDatas)
        {
            var tempNode = NodeFactory.CreateSpeechNode(nodeData.Position, nodeData.DialogueText);
            tempNode.GUID = nodeData.Guid;

            // Загружаем аудиофайл если есть GUID
            if (!string.IsNullOrEmpty(nodeData.AudioClipGuid))
            {
                tempNode.AudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AssetDatabase.GUIDToAssetPath(nodeData.AudioClipGuid));
            }

            targetGraphView.AddElement(tempNode);
        }

        // Создаем OptionNode
        foreach (var nodeData in containerCache.OptionNodeDatas)
        {
            var tempNode = NodeFactory.CreateOptionNode(nodeData.Position, nodeData.ResponseText);
            tempNode.GUID = nodeData.Guid;

            // Загружаем аудиофайл если есть GUID
            if (!string.IsNullOrEmpty(nodeData.AudioClipGuid))
            {
                tempNode.AudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AssetDatabase.GUIDToAssetPath(nodeData.AudioClipGuid));
            }

            targetGraphView.AddElement(tempNode);
        }
    }

    /// <summary>
    /// Восстановление связей между узлами
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

                    // Находим соответствующий порт
                    Port outputPort = null;

                    if (Nodes[i] is SpeechNode speechNode)
                    {
                        // Ищем порт с нужным именем
                        foreach (var port in speechNode.outputContainer.Children())
                        {
                            if (port is Port portElement && portElement.portName == connection.PortName)
                            {
                                outputPort = portElement;
                                break;
                            }
                        }

                        // Если порт не найден, создаем его
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
        foreach (var exposedProperty in containerCache.ExposedProperties)
        {
            targetGraphView.AddPropertyToBlackBoard(exposedProperty);
        }
    }

    /// <summary>
    /// Очистка текущего графа перед загрузкой
    /// </summary>
    private void ClearGraph()
    {
        // Удаляем все узлы кроме стартового
        foreach (var node in Nodes.Where(node => !node.EntryPoint).ToList())
        {
            // Удаляем связанные связи
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