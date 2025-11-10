using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CreateConnectionCommand : GraphCommand
{
    private Edge edge;
    private Port outputPort;
    private Port inputPort;

    public CreateConnectionCommand(DialogueGraphView graphView, Edge edge)
        : base(graphView)
    {
        this.edge = edge;
        this.outputPort = edge.output;
        this.inputPort = edge.input;
    }

    public override void Execute()
    {
        if (edge != null && edge.parent != null)
        {
            graphView.RemoveElement(edge);
            graphView.MarkUnsavedChangeWithoutFile();
        }
    }

    public override void Undo()
    {
        if (outputPort != null && inputPort != null)
        {
            Edge newEdge = new Edge { output = outputPort, input = inputPort };
            outputPort.Connect(newEdge);
            inputPort.Connect(newEdge);
            graphView.Add(newEdge);
            graphView.MarkUnsavedChangeWithoutFile();
        }
    }
}