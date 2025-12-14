using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class ContextualDeleteCommand : GraphCommand
{
    private List<ISelectable> _nodesToDelete;
    private List<Edge> _edgesToDelete;
    // Для Undo
    private Dictionary<string, Vector2> _nodePositions = new Dictionary<string, Vector2>();
    private List<NodeLinkData> _edgeConnections = new List<NodeLinkData>();
    private Dictionary<string, WireNodeConnectionData> _wireNodeConnections = new Dictionary<string, WireNodeConnectionData>();
    private Dictionary<ISelectable, string> _elementGuids = new Dictionary<ISelectable, string>();
    private List<string> _deletedNodeGuids = new List<string>();
    private List<string> _deletedEdgeGuids = new List<string>();

    private Dictionary<string, string> _serializedNodeData = new Dictionary<string, string>();

    [System.Serializable]
    public class WireNodeConnectionData
    {
        public Vector2 Position;
        public List<string> ConnectedInputGuids = new List<string>();
        public List<string> ConnectedOutputGuids = new List<string>();
    }

    public ContextualDeleteCommand(DialogueGraphView graphView, List<ISelectable> nodesToDelete, List<Edge> edgesToDelete)
        : base(graphView)
    {
        _nodesToDelete = nodesToDelete;
        _edgesToDelete = edgesToDelete;
    }

    public override void Execute()
    {
        // Сохраняем информацию для отмены
        SaveStateForUndo();

        // Удаляем ребра сначала
        foreach (var edge in _edgesToDelete.Distinct().ToList())
        {
            DeleteEdge(edge);
            _deletedEdgeGuids.Add(edge.GetHashCode().ToString());
        }

        // Удаляем узлы
        foreach (var element in _nodesToDelete)
        {
            if (element is BaseNode node)
            {
                // Обработка WireNode
                if (node is WireNode wireNode)
                {
                    RemoveWireNode(wireNode);
                    _deletedNodeGuids.Add(wireNode.GUID);
                    continue;
                }

                // Автоматически удаляем все соединения, связанные с удаляемым узлом
                var edgesToRemove = graphView.edges
                    .Where(e => e.output?.node == node || e.input?.node == node)
                    .ToList();

                foreach (var edge in edgesToRemove)
                {
                    if (edge != null && !_edgesToDelete.Contains(edge))
                    {
                        DeleteEdge(edge);
                        _deletedEdgeGuids.Add(edge.GetHashCode().ToString());
                    }
                }

                // Удаляем сам узел
                if (node.parent != null && !node.EntryPoint)
                {
                    graphView.RemoveElement(node);
                    _deletedNodeGuids.Add(node.GUID);
                }
            }
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    private void RemoveWireNode(WireNode wireNode)
    {
        // Находим все входящие и исходящие соединения
        var inputEdges = wireNode.inputContainer[0].Query<Edge>().ToList();
        var outputEdges = wireNode.outputContainer[0].Query<Edge>().ToList();

        // Собираем соединения для восстановления при отмене
        var connectedInputs = inputEdges.Select(e => e.output.node).OfType<BaseNode>().Select(n => n.GUID).ToList();
        var connectedOutputs = outputEdges.Select(e => e.input.node).OfType<BaseNode>().Select(n => n.GUID).ToList();

        _wireNodeConnections[wireNode.GUID] = new WireNodeConnectionData
        {
            Position = wireNode.GetPosition().position,
            ConnectedInputGuids = connectedInputs,
            ConnectedOutputGuids = connectedOutputs
        };

        // Удаляем все ребра, связанные с этим WireNode
        foreach (var edge in inputEdges.Concat(outputEdges).Distinct().ToList())
        {
            DeleteEdge(edge);
            _deletedEdgeGuids.Add(edge.GetHashCode().ToString());
        }

        // Удаляем сам WireNode
        if (wireNode.parent != null)
        {
            graphView.RemoveElement(wireNode);
        }
    }

    private void SaveStateForUndo()
    {
        // Сохраняем позиции узлов
        foreach (var element in _nodesToDelete)
        {
            if (element is BaseNode node && !(node is WireNode))
            {
                _nodePositions[node.GUID] = node.GetPosition().position;
                _elementGuids[element] = node.GUID;
            }
        }

        // Сохраняем соединения для ребер
        foreach (var edge in _edgesToDelete)
        {
            if (edge.output?.node is BaseNode outputNode && edge.input?.node is BaseNode inputNode)
            {
                _edgeConnections.Add(new NodeLinkData
                {
                    BaseNodeGuid = outputNode.GUID,
                    PortName = edge.output.portName,
                    TargetNodeGuid = inputNode.GUID
                });
            }
        }

        foreach (var element in _nodesToDelete)
        {
            if (element is BaseNode node && !(node is WireNode))
            {
                _nodePositions[node.GUID] = node.GetPosition().position;
                _elementGuids[element] = node.GUID;
                // Сохраняем данные узла
                _serializedNodeData[node.GUID] = node.SerializeNodeData();
            }
        }
    }

    private void DeleteEdge(Edge edge)
    {
        if (edge == null || edge.parent == null)
            return;

        edge.output?.Disconnect(edge);
        edge.input?.Disconnect(edge);
        graphView.RemoveElement(edge);
    }

    public override void Undo()
    {
        // Восстанавливаем узлы
        foreach (var kvp in _nodePositions)
        {
            var guid = kvp.Key;
            var position = kvp.Value;

            // Пытаемся найти сохраненный узел в кэше
            var node = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == guid) as BaseNode;

            if (node == null)
            {
                Debug.LogWarning($"Node with GUID {guid} not found for undo operation");
                continue;
            }

            // Восстанавливаем позицию узла
            node.SetPosition(new Rect(position, graphView.DefaultNodeSize));

            // Если узел был удален из графа - добавляем его обратно
            if (node.parent == null)
            {
                graphView.AddElement(node);
            }
        }

        // Восстанавливаем WireNodes
        foreach (var connectionData in _wireNodeConnections)
        {
            var wireNode = new WireNode();
            wireNode.Initialize(connectionData.Value.Position);
            wireNode.GUID = connectionData.Key;
            graphView.AddElement(wireNode);
        }

        // Восстанавливаем ребра
        foreach (var connection in _edgeConnections)
        {
            var outputNode = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == connection.BaseNodeGuid) as BaseNode;
            var inputNode = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == connection.TargetNodeGuid) as BaseNode;

            if (outputNode == null || inputNode == null)
                continue;

            Port outputPort = null;
            Port inputPort = null;

            // Ищем output порт
            foreach (Port port in outputNode.outputContainer.Children())
            {
                if (port.portName == connection.PortName)
                {
                    outputPort = port;
                    break;
                }
            }

            // Находим input порт
            if (inputNode.inputContainer.childCount > 0)
            {
                inputPort = inputNode.inputContainer[0] as Port;
            }

            // Создаем и добавляем ребро
            if (outputPort != null && inputPort != null)
            {
                var edge = new Edge { output = outputPort, input = inputPort };
                outputPort.Connect(edge);
                inputPort.Connect(edge);
                graphView.Add(edge);
            }
        }

        // Восстанавливаем соединения для WireNodes
        foreach (var connectionData in _wireNodeConnections)
        {
            var wireNode = graphView.nodes.ToList().FirstOrDefault(n => n is WireNode wn && wn.GUID == connectionData.Key) as WireNode;
            if (wireNode == null) continue;

            // Восстанавливаем входящие соединения
            foreach (var inputGuid in connectionData.Value.ConnectedInputGuids)
            {
                var outputNode = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == inputGuid) as BaseNode;
                if (outputNode != null && wireNode.inputContainer.childCount > 0)
                {
                    var inputPort = wireNode.inputContainer[0] as Port;
                    var outputPort = outputNode.outputContainer.Children().OfType<Port>().FirstOrDefault();

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = new Edge { output = outputPort, input = inputPort };
                        outputPort.Connect(edge);
                        inputPort.Connect(edge);
                        graphView.Add(edge);
                    }
                }
            }

            // Восстанавливаем исходящие соединения
            foreach (var outputGuid in connectionData.Value.ConnectedOutputGuids)
            {
                var inputNode = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == outputGuid) as BaseNode;
                if (inputNode != null && wireNode.outputContainer.childCount > 0)
                {
                    var outputPort = wireNode.outputContainer[0] as Port;
                    var inputPort = inputNode.inputContainer.Children().OfType<Port>().FirstOrDefault();

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = new Edge { output = outputPort, input = inputPort };
                        outputPort.Connect(edge);
                        inputPort.Connect(edge);
                        graphView.Add(edge);
                    }
                }
            }

        }

        foreach (var kvp in _nodePositions)
        {
            var guid = kvp.Key;
            var position = kvp.Value;
            var node = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == guid) as BaseNode;

            if (node == null)
            {
                // Если узел не найден, создаем новый того же типа
                var originalNode = _nodesToDelete.FirstOrDefault(n => n is BaseNode bn && bn.GUID == guid) as BaseNode;
                if (originalNode != null)
                {
                    node = NodeFactory.CreateNode(originalNode.GetType(), position);
                    node.GUID = guid;
                }
            }

            if (node != null)
            {
                node.SetPosition(new Rect(position, graphView.DefaultNodeSize));

                // Восстанавливаем данные узла
                if (_serializedNodeData.TryGetValue(guid, out var data) && !string.IsNullOrEmpty(data))
                {
                    node.DeserializeNodeData(data);
                }

                if (node.parent == null)
                {
                    graphView.AddElement(node);
                }
            }
        }

        // Восстанавливаем выделение
        graphView.ClearSelection();
        foreach (var guid in _deletedNodeGuids)
        {
            var node = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == guid) as BaseNode;
            if (node != null)
            {
                graphView.AddToSelection(node);
            }
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }
}