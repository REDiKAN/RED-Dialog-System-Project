using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[System.Serializable]
public class SerializedConnection
{
    public string sourceGuid;
    public string targetGuid;
    public string portName;
}

public class DuplicateNodesCommand : GraphCommand
{
    private List<BaseNode> duplicatedNodes = new List<BaseNode>();
    private List<Edge> createdEdges = new List<Edge>();
    private Vector2 pastePosition;
    private Dictionary<string, string> guidMap = new Dictionary<string, string>();
    private List<BaseNode> originalNodes = new List<BaseNode>();
    private List<SerializedConnection> internalConnections = new List<SerializedConnection>();

    public DuplicateNodesCommand(DialogueGraphView graphView, List<BaseNode> nodesToDuplicate, Vector2 pastePosition)
        : base(graphView)
    {
        this.pastePosition = pastePosition;
        this.originalNodes = nodesToDuplicate;

        // Собираем внутренние связи между выделенными узлами
        foreach (var edge in graphView.edges.ToList())
        {
            var outputNode = edge.output?.node as BaseNode;
            var inputNode = edge.input?.node as BaseNode;

            if (outputNode != null && inputNode != null &&
                nodesToDuplicate.Contains(outputNode) && nodesToDuplicate.Contains(inputNode))
            {
                internalConnections.Add(new SerializedConnection
                {
                    sourceGuid = outputNode.GUID,
                    targetGuid = inputNode.GUID,
                    portName = edge.output.portName
                });
            }
        }
    }

    public override void Execute()
    {
        // Вычисляем центр выделения для правильного позиционирования
        Vector2 selectionCenter = Vector2.zero;
        if (originalNodes.Count > 0)
        {
            foreach (var node in originalNodes)
            {
                selectionCenter += node.GetPosition().position;
            }
            selectionCenter /= originalNodes.Count;
        }

        // Генерируем новые GUID для копий
        foreach (var node in originalNodes)
        {
            guidMap[node.GUID] = Guid.NewGuid().ToString();
        }

        // Создаем копии узлов
        foreach (var originalNode in originalNodes)
        {
            // Пропускаем EntryPoint узлы (дублирование запрещено)
            if (originalNode.EntryPoint) continue;

            // Создаем копию узла того же типа
            Type nodeType = originalNode.GetType();
            Vector2 nodePosition = originalNode.GetPosition().position;
            Vector2 offset = nodePosition - selectionCenter;
            Vector2 newNodePosition = pastePosition + offset;

            var newNode = NodeFactory.CreateNode(nodeType, newNodePosition);
            if (newNode == null) continue;

            // Присваиваем новый GUID
            newNode.GUID = guidMap[originalNode.GUID];

            // Копируем данные узла через сериализацию/десериализацию
            string nodeData = originalNode.SerializeNodeData();
            if (!string.IsNullOrEmpty(nodeData))
            {
                newNode.DeserializeNodeData(nodeData);
            }

            // Сбрасываем флаг EntryPoint, если он установлен
            if (newNode.EntryPoint)
            {
                newNode.EntryPoint = false;
                // Восстанавливаем возможности для удаления и перемещения
                newNode.capabilities |= Capabilities.Deletable;
                newNode.capabilities |= Capabilities.Movable;
            }

            graphView.AddElement(newNode);
            duplicatedNodes.Add(newNode);
        }

        // Создаем связи между дубликатами
        foreach (var connection in internalConnections)
        {
            if (guidMap.TryGetValue(connection.sourceGuid, out string newSourceGuid) &&
                guidMap.TryGetValue(connection.targetGuid, out string newTargetGuid))
            {
                var sourceNode = duplicatedNodes.FirstOrDefault(n => n.GUID == newSourceGuid);
                var targetNode = duplicatedNodes.FirstOrDefault(n => n.GUID == newTargetGuid);

                if (sourceNode != null && targetNode != null)
                {
                    Port outputPort = null;
                    Port inputPort = null;

                    // Ищем output порт по имени
                    foreach (Port port in sourceNode.outputContainer.Children())
                    {
                        if (port is Port portElement && portElement.portName == connection.portName)
                        {
                            outputPort = portElement;
                            break;
                        }
                    }

                    // Ищем input порт
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

        // Выделяем новые узлы
        graphView.ClearSelection();
        foreach (var node in duplicatedNodes)
        {
            graphView.AddToSelection(node);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        // Удаляем созданные связи
        foreach (var edge in createdEdges.ToList())
        {
            if (edge != null && edge.parent != null)
            {
                graphView.RemoveElement(edge);
            }
        }

        // Удаляем дублированные узлы
        foreach (var node in duplicatedNodes.ToList())
        {
            if (node != null && node.parent != null)
            {
                // Удаляем все связи, связанные с этим узлом
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