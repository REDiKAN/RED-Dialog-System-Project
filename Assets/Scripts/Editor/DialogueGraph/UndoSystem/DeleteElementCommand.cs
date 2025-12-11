using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using System;
using DialogueSystem;

public class DeleteElementCommand : GraphCommand
{
    private ISelectable element;
    private Vector2 position;
    private System.Type elementType;
    private string guid;
    private List<Edge> connections = new List<Edge>();
    private List<NodeLinkData> edgeData = new List<NodeLinkData>();

    public DeleteElementCommand(DialogueGraphView graphView, ISelectable element)
        : base(graphView)
    {
        this.element = element;
        if (element is BaseNode node)
        {
            elementType = node.GetType();
            guid = node.GUID;
            position = node.GetPosition().position;
        }
    }

    public override void Execute()
    {
        if (element is BaseNode node)
        {
            // Сохраняем все соединения перед удалением
            connections = graphView.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();

            // Сохраняем данные о соединениях для восстановления
            foreach (var edge in connections)
            {
                if (edge.output?.node is BaseNode outputNode && edge.input?.node is BaseNode inputNode)
                {
                    edgeData.Add(new NodeLinkData
                    {
                        BaseNodeGuid = outputNode.GUID,
                        PortName = edge.output.portName,
                        TargetNodeGuid = inputNode.GUID
                    });
                }
            }

            // Удаляем все соединения
            foreach (var edge in connections.ToList())
            {
                graphView.RemoveElement(edge);
            }

            // Удаляем сам узел
            graphView.RemoveElement(node);
        }
        else if (element is Edge edge)
        {
            // Для удаления отдельного соединения
            graphView.RemoveElement(edge);
            connections.Add(edge);
        }
    }
    public override void Undo()
    {
        if (elementType != null && typeof(BaseNode).IsAssignableFrom(elementType))
        {
            // Воссоздаём узел
            var node = NodeFactory.CreateNode(elementType, position);
            node.GUID = guid;
            graphView.AddElement(node);

            // Восстанавливаем соединения
            foreach (var linkData in edgeData)
            {
                var outputNode = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == linkData.BaseNodeGuid) as BaseNode;
                var inputNode = graphView.nodes.ToList().FirstOrDefault(n => n is BaseNode bn && bn.GUID == linkData.TargetNodeGuid) as BaseNode;

                if (outputNode != null && inputNode != null)
                {
                    Port outputPort = FindPortByName(outputNode, linkData.PortName);
                    Port inputPort = inputNode.inputContainer.Children().OfType<Port>().FirstOrDefault();

                    if (outputPort != null && inputPort != null)
                    {
                        var newEdge = new Edge
                        {
                            output = outputPort,
                            input = inputPort
                        };
                        outputPort.Connect(newEdge);
                        inputPort.Connect(newEdge);
                        graphView.Add(newEdge);
                    }
                }
            }
        }
        else if (connections.Count > 0)
        {
            // Восстанавливаем соединение
            foreach (var edge in connections)
            {
                if (edge.output != null && edge.input != null && edge.parent == null)
                {
                    graphView.Add(edge);
                }
            }
        }
    }

    private Port FindPortByName(BaseNode node, string portName)
    {
        return node.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == portName);
    }
}