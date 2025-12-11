using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class CreateNodeCommand : GraphCommand
{
    private BaseNode node;
    private Vector2 position;
    private Type nodeType;
    private string guid;
    private List<Edge> createdEdges = new List<Edge>();

    public CreateNodeCommand(DialogueGraphView graphView, Type nodeType, Vector2 position)
        : base(graphView)
    {
        this.nodeType = nodeType;
        this.position = position;
        this.guid = Guid.NewGuid().ToString();
    }

    public override void Execute()
    {
        node = NodeFactory.CreateNode(nodeType, position);
        node.GUID = guid;
        graphView.AddElement(node);
    }

    public override void Undo()
    {
        if (node != null && node.parent != null)
        {
            // Сохраняем все соединения перед удалением узла
            createdEdges = graphView.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();

            // Удаляем все соединения
            foreach (var edge in createdEdges.ToList())
            {
                graphView.RemoveElement(edge);
            }

            // Удаляем сам узел
            graphView.RemoveElement(node);
        }
    }
}