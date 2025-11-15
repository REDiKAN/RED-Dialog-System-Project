using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[System.Serializable]
public class ClipboardData
{
    public List<SerializedNode> nodes = new List<SerializedNode>();
    public List<SerializedConnection> connections = new List<SerializedConnection>();
    public Vector2 center;
    public Vector2 size;
}

[System.Serializable]
public class SerializedNode
{
    public string type;
    public string guid;
    public Vector2 position;
    public string nodeData;
}

public class PasteNodesCommand : GraphCommand
{
    private List<BaseNode> pastedNodes = new List<BaseNode>();
    private List<Edge> createdEdges = new List<Edge>();
    private Vector2 pastePosition;
    private DialogueGraphView.ClipboardData clipboardData;
    private Dictionary<string, string> guidMap = new Dictionary<string, string>();

    public PasteNodesCommand(DialogueGraphView graphView, DialogueGraphView.ClipboardData clipboardData, Vector2 pastePosition)
        : base(graphView)
    {
        this.clipboardData = clipboardData;
        this.pastePosition = pastePosition;
    }

    public override void Execute()
    {
        // Создание узлов
        foreach (var serializedNode in clipboardData.nodes)
        {
            // Генерация нового GUID
            string newGuid = Guid.NewGuid().ToString();
            guidMap[serializedNode.guid] = newGuid;

            // Поиск типа узла
            Type nodeType = Type.GetType($"DialogueSystem.{serializedNode.type}");
            if (nodeType == null)
            {
                // Пробуем без namespace
                nodeType = Type.GetType(serializedNode.type);
                if (nodeType == null)
                {
                    Debug.LogWarning($"Unknown node type: {serializedNode.type}");
                    continue;
                }
            }

            // Создание узла
            Vector2 nodePosition = new Vector2(
                pastePosition.x + (serializedNode.position.x - clipboardData.center.x),
                pastePosition.y + (serializedNode.position.y - clipboardData.center.y)
            );

            var newNode = NodeFactory.CreateNode(nodeType, nodePosition);
            if (newNode == null) continue;

            newNode.GUID = newGuid;
            newNode.DeserializeNodeData(serializedNode.nodeData);

            graphView.AddElement(newNode);
            pastedNodes.Add(newNode);
        }

        // Создание связей
        foreach (var connection in clipboardData.connections)
        {
            if (guidMap.TryGetValue(connection.sourceGuid, out string newSourceGuid) &&
                guidMap.TryGetValue(connection.targetGuid, out string newTargetGuid))
            {
                var sourceNode = pastedNodes.FirstOrDefault(n => n.GUID == newSourceGuid);
                var targetNode = pastedNodes.FirstOrDefault(n => n.GUID == newTargetGuid);

                if (sourceNode != null && targetNode != null)
                {
                    Port outputPort = null;
                    Port inputPort = null;

                    // Поиск output порта по имени
                    foreach (Port port in sourceNode.outputContainer.Children())
                    {
                        if (port is Port portElement && portElement.portName == connection.portName)
                        {
                            outputPort = portElement;
                            break;
                        }
                    }

                    // Поиск первого input порта
                    if (targetNode.inputContainer.childCount > 0)
                    {
                        inputPort = targetNode.inputContainer[0] as Port;
                    }

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = new Edge { output = outputPort, input = inputPort };
                        outputPort.Connect(edge);
                        inputPort.Connect(edge);
                        graphView.Add(edge);
                        createdEdges.Add(edge);
                    }
                }
            }
        }

        // Выделение вставленных узлов
        graphView.ClearSelection();
        foreach (var node in pastedNodes)
        {
            graphView.AddToSelection(node);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        // Удаление связей
        foreach (var edge in createdEdges.ToList())
        {
            if (edge != null && edge.parent != null)
            {
                graphView.RemoveElement(edge);
            }
        }

        // Удаление узлов
        foreach (var node in pastedNodes.ToList())
        {
            if (node != null && node.parent != null)
            {
                // Удаление всех связей, связанных с этим узлом
                var edgesToRemove = graphView.edges
                    .Where(e => e.input.node == node || e.output.node == node)
                    .ToList();

                foreach (var edge in edgesToRemove)
                {
                    graphView.RemoveElement(edge);
                }

                graphView.RemoveElement(node);
            }
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }
}