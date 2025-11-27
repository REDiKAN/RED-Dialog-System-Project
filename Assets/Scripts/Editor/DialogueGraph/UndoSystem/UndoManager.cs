public class UndoManager
{
    private readonly DialogueGraphView graphView;

    public UndoManager(DialogueGraphView graphView)
    {
        this.graphView = graphView;
    }

    // Выполняем команду напрямую без сохранения для отмены
    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
    }

    // Отключаем функциональность отмены
    public void Undo() { }

    // Отключаем функциональность повтора
    public void Redo() { }

    // Очищаем стеки (хотя они больше не используются)
    public void ClearStacks() { }
}