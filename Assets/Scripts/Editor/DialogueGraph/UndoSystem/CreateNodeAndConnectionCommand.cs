using System;
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

    public CreateNodeAndConnectionCommand(DialogueGraphView graphView, Type nodeType, Vector2 position, Port outputPort)
        : base(graphView)
    {
        _nodeType = nodeType;
        _position = position;
        _outputPort = outputPort;
        _newGuid = System.Guid.NewGuid().ToString();
    }

    public override void Execute()
    {
        // Создаем новый узел
        _createdNode = NodeFactory.CreateNode(_nodeType, _position);
        if (_createdNode == null) return;

        _createdNode.GUID = _newGuid;
        graphView.AddElement(_createdNode);

        // Находим подходящий input port у нового узла
        Port inputPort = null;
        if (_createdNode.inputContainer.childCount > 0)
        {
            inputPort = _createdNode.inputContainer[0] as Port;
        }

        // Создаем соединение, если найден input port
        if (inputPort != null)
        {
            _createdEdge = new Edge { output = _outputPort, input = inputPort };
            _outputPort.Connect(_createdEdge);
            inputPort.Connect(_createdEdge);
            graphView.Add(_createdEdge);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        // Удаляем соединение
        if (_createdEdge != null && _createdEdge.parent != null)
        {
            graphView.RemoveElement(_createdEdge);
        }

        // Удаляем узел
        if (_createdNode != null && _createdNode.parent != null)
        {
            // Удаляем все связанные соединения
            var edgesToRemove = graphView.edges
                .Where(e => e.input.node == _createdNode || e.output.node == _createdNode)
                .ToList();

            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }

            graphView.RemoveElement(_createdNode);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }
}