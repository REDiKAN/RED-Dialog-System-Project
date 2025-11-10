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
    // Изменено: вместо хранения связей как NodeLinkData, храним реальные Edge
    private List<Edge> connections = new List<Edge>();

    public DeleteElementCommand(DialogueGraphView graphView, ISelectable element)
        : base(graphView)
    {
        this.element = element;

        if (element is BaseNode node)
        {
            elementType = node.GetType();
            guid = node.GUID;
            position = node.GetPosition().position;

            // Сохраняем реальные связи вместо данных из containerCache
            connections = graphView.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();
        }
    }

    public override void Execute()
    {
        if (element is BaseNode node)
        {
            // Сохраняем связи перед удалением
            connections = graphView.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();

            // Удаляем все связи узла
            foreach (var edge in connections.ToList())
            {
                graphView.RemoveElement(edge);
            }

            graphView.RemoveElement(node);
        }
        else if (element is Edge edge)
        {
            graphView.RemoveElement(edge);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        if (elementType != null && typeof(BaseNode).IsAssignableFrom(elementType))
        {
            var node = NodeFactory.CreateNode(elementType, position);
            node.GUID = guid;
            graphView.AddElement(node);

            // Восстанавливаем связи
            foreach (var edge in connections)
            {
                // Преобразуем узлы в BaseNode для доступа к GUID
                BaseNode edgeOutputNode = edge.output.node as BaseNode;
                BaseNode edgeInputNode = edge.input.node as BaseNode;

                // Пропускаем, если преобразование не удалось
                if (edgeOutputNode == null || edgeInputNode == null)
                    continue;

                // Ищем соответствующие узлы в графе по GUID
                var outputNode = graphView.nodes.ToList().FirstOrDefault(n =>
                    n is BaseNode bn && bn.GUID == edgeOutputNode.GUID);
                var inputNode = graphView.nodes.ToList().FirstOrDefault(n =>
                    n is BaseNode bn && bn.GUID == edgeInputNode.GUID);

                // Проверяем, что нашли узлы и они являются BaseNode
                if (outputNode is BaseNode outputBaseNode &&
                    inputNode is BaseNode inputBaseNode)
                {
                    // Находим соответствующие порты
                    Port outputPort = FindPortByName(outputBaseNode, edge.output.portName);
                    Port inputPort = FindPortByName(inputBaseNode, edge.input.portName);

                    // Создаем и добавляем новое соединение
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
        graphView.MarkUnsavedChangeWithoutFile();
    }

    private Port FindPortByName(BaseNode node, string portName)
    {
        return node.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == portName) ??
               node.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portName == portName);
    }
}