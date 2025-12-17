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
    private ClipboardData clipboardData;
    private Dictionary<string, string> guidMap = new Dictionary<string, string>();

    public PasteNodesCommand(DialogueGraphView graphView, ClipboardData clipboardData, Vector2 pastePosition)
        : base(graphView)
    {
        this.clipboardData = clipboardData;
        this.pastePosition = pastePosition;
    }

    public override void Execute()
    {
        // Создаем маппинг старых GUID на новые
        foreach (var serializedNode in clipboardData.nodes)
        {
            string newGuid = Guid.NewGuid().ToString();
            guidMap[serializedNode.guid] = newGuid;
        }

        // Создаем узлы
        foreach (var serializedNode in clipboardData.nodes)
        {
            // Находим тип узла по полному имени (включая namespace)
            Type nodeType = Type.GetType(serializedNode.type);
            if (nodeType == null)
            {
                Debug.LogWarning($"Unknown node type: {serializedNode.type}");
                continue;
            }

            // Позиция нового узла с учетом смещения относительно центра
            Vector2 nodePosition = new Vector2(
                pastePosition.x + (serializedNode.position.x - clipboardData.center.x),
                pastePosition.y + (serializedNode.position.y - clipboardData.center.y)
            );

            // Создаем узел
            var newNode = NodeFactory.CreateNode(nodeType, nodePosition);
            if (newNode == null) continue;

            // Устанавливаем новый GUID
            newNode.GUID = guidMap[serializedNode.guid];

            // Десериализуем данные узла
            newNode.DeserializeNodeData(serializedNode.nodeData);

            // Добавляем узел в граф
            graphView.AddElement(newNode);
            pastedNodes.Add(newNode);
        }

        // Создаем связи между узлами
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

                    // Находим output порт по имени
                    foreach (Port port in sourceNode.outputContainer.Children())
                    {
                        if (port is Port portElement && portElement.portName == connection.portName)
                        {
                            outputPort = portElement;
                            break;
                        }
                    }

                    // Находим первый input порт
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

        // Выделяем вставленные узлы
        graphView.ClearSelection();
        foreach (var node in pastedNodes)
        {
            graphView.AddToSelection(node);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        // Удаляем связи
        foreach (var edge in createdEdges.ToList())
        {
            if (edge != null && edge.parent != null)
            {
                graphView.RemoveElement(edge);
            }
        }

        // Удаляем узлы
        foreach (var node in pastedNodes.ToList())
        {
            if (node != null && node.parent != null)
            {
                // Удаляем также все связи, связанные с этим узлом
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