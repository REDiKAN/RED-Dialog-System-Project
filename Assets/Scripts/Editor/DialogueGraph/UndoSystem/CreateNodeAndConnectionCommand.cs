using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CreateNodeAndConnectionCommand : GraphCommand
{
    private Type _nodeType;
    private Vector2 _position;
    private Port _outputPort;
    private BaseNode _createdNode;
    private Edge _createdEdge;
    private string _newGuid;
    private List<Edge> _edgesToRemove = new List<Edge>();

    public CreateNodeAndConnectionCommand(DialogueGraphView graphView, Type nodeType, Vector2 position, Port outputPort)
        : base(graphView)
    {
        _nodeType = nodeType;
        _position = position;
        _outputPort = outputPort;
        _newGuid = Guid.NewGuid().ToString();
    }

    public override void Execute()
    {
        // Определяем тип создаваемого узла
        bool isCreatingOptionNode = IsOptionNodeType(_nodeType);
        bool isCreatingSpeechNode = IsSpeechNodeType(_nodeType);

        // Собираем все существующие соединения порта
        var existingConnections = _outputPort.connections.ToList();

        // Для портов с Single capacity удаляем все соединения
        if (_outputPort.capacity == Port.Capacity.Single)
        {
            _edgesToRemove.AddRange(existingConnections);
        }
        else // Для Multi capacity обрабатываем конфликты по типам узлов
        {
            foreach (var edge in existingConnections)
            {
                var targetNode = edge.input?.node as BaseNode;
                if (targetNode == null) continue;

                bool isExistingOptionNode = IsOptionNode(targetNode);
                bool isExistingSpeechNode = IsSpeechNode(targetNode);

                // Если создаем Speech node, а существующее соединение с Option node - удаляем
                if (isCreatingSpeechNode && isExistingOptionNode)
                {
                    _edgesToRemove.Add(edge);
                }
                // Если создаем Option node, а существующее соединение с Speech node - удаляем
                else if (isCreatingOptionNode && isExistingSpeechNode)
                {
                    _edgesToRemove.Add(edge);
                }
                // Если создаем Speech node, а существующее соединение уже с другим Speech - удаляем
                else if (isCreatingSpeechNode && isExistingSpeechNode)
                {
                    _edgesToRemove.Add(edge);
                }
            }
        }

        // Удаляем все конфликтующие соединения
        foreach (var edge in _edgesToRemove.Distinct().ToList())
        {
            if (edge != null)
            {
                _outputPort.Disconnect(edge);
                edge.input?.Disconnect(edge);
                if (edge.parent != null)
                {
                    graphView.RemoveElement(edge);
                }
            }
        }

        // Создаем новый узел
        _createdNode = NodeFactory.CreateNode(_nodeType, _position);
        if (_createdNode == null) return;
        _createdNode.GUID = _newGuid;
        graphView.AddElement(_createdNode);

        // Создаем соединение
        if (_createdNode.inputContainer.childCount > 0)
        {
            Port inputPort = _createdNode.inputContainer[0] as Port;
            if (inputPort != null)
            {
                _createdEdge = new Edge { output = _outputPort, input = inputPort };
                _outputPort.Connect(_createdEdge);
                inputPort.Connect(_createdEdge);
                graphView.Add(_createdEdge);
            }
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        // Удаляем соединение
        if (_createdEdge != null && _createdEdge.parent != null)
        {
            _createdEdge.output?.Disconnect(_createdEdge);
            _createdEdge.input?.Disconnect(_createdEdge);
            graphView.RemoveElement(_createdEdge);
        }

        // Удаляем узел
        if (_createdNode != null && _createdNode.parent != null)
        {
            // Удаляем все соединения узла
            var edgesToRemove = graphView.edges
                .Where(e => e.input.node == _createdNode || e.output.node == _createdNode)
                .ToList();
            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }
            graphView.RemoveElement(_createdNode);
        }

        // Восстанавливаем удаленные соединения
        foreach (var edge in _edgesToRemove)
        {
            if (edge != null && edge.output != null && edge.input != null)
            {
                edge.output.Connect(edge);
                edge.input.Connect(edge);
                if (edge.parent == null)
                {
                    graphView.Add(edge);
                }
            }
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    // Вспомогательные методы для определения типов узлов
    private static bool IsSpeechNodeType(Type nodeType)
    {
        return nodeType == typeof(SpeechNode) ||
               nodeType == typeof(SpeechNodeText) ||
               nodeType == typeof(SpeechNodeAudio) ||
               nodeType == typeof(SpeechNodeImage) ||
               nodeType == typeof(SpeechNodeRandText);
    }

    private static bool IsOptionNodeType(Type nodeType)
    {
        return nodeType == typeof(OptionNode) ||
               nodeType == typeof(OptionNodeText) ||
               nodeType == typeof(OptionNodeAudio) ||
               nodeType == typeof(OptionNodeImage);
    }

    private static bool IsSpeechNode(BaseNode node)
    {
        return node is SpeechNode ||
               node is SpeechNodeText ||
               node is SpeechNodeAudio ||
               node is SpeechNodeImage ||
               node is SpeechNodeRandText;
    }

    private static bool IsOptionNode(BaseNode node)
    {
        return node is OptionNode ||
               node is OptionNodeText ||
               node is OptionNodeAudio ||
               node is OptionNodeImage;
    }
}