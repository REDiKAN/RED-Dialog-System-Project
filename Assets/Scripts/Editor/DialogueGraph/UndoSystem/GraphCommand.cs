public abstract class GraphCommand : ICommand
{
    protected DialogueGraphView graphView;

    public GraphCommand(DialogueGraphView graphView)
    {
        this.graphView = graphView;
    }

    public abstract void Execute();
    public abstract void Undo();
    public virtual void Redo() => Execute();
}