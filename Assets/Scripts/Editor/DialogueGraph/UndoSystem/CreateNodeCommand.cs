using UnityEngine;
using System;
using System.Linq;

public class CreateNodeCommand : GraphCommand
{
    private BaseNode node;
    private Vector2 position;
    private Type nodeType;
    private string guid;

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
        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        if (node != null && node.parent != null)
        {
            // Удаляем все связи узла
            var edgesToRemove = graphView.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();

            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }

            graphView.RemoveElement(node);
            graphView.MarkUnsavedChangeWithoutFile();
        }
    }
}