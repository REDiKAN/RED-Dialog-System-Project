using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CreateNodeAndConnectionCommand : GraphCommand
{
    private BaseNode newNode;
    private Port draggedOutputPort;
    private Edge createdEdge;

    public CreateNodeAndConnectionCommand(DialogueGraphView graphView, BaseNode newNode, Port draggedOutputPort)
        : base(graphView)
    {
        this.newNode = newNode;
        this.draggedOutputPort = draggedOutputPort;
    }

    public override void Execute()
    {
        // Удаляем узел и связь
        if (createdEdge != null && createdEdge.parent != null)
        {
            graphView.RemoveElement(createdEdge);
        }

        if (newNode != null && newNode.parent != null)
        {
            graphView.RemoveElement(newNode);
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }

    public override void Undo()
    {
        // Восстанавливаем узел
        graphView.AddElement(newNode);

        // Восстанавливаем связь
        if (draggedOutputPort != null && newNode.inputContainer.childCount > 0)
        {
            Port inputPort = newNode.inputContainer[0] as Port;
            if (inputPort != null)
            {
                createdEdge = new Edge { output = draggedOutputPort, input = inputPort };
                draggedOutputPort.Connect(createdEdge);
                inputPort.Connect(createdEdge);
                graphView.Add(createdEdge);
            }
        }

        graphView.MarkUnsavedChangeWithoutFile();
    }
}