using System.Collections.Generic;
using System.Linq;

public class UndoManager
{
    private readonly DialogueGraphView graphView;
    // Убираем readonly с этих полей, чтобы их можно было переназначать
    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();
    private readonly int maxStackSize = 50;

    public UndoManager(DialogueGraphView graphView)
    {
        this.graphView = graphView;
    }

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        AddToUndoStack(command);
        ClearRedoStack();
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;

        var command = undoStack.Pop();
        command.Undo();
        redoStack.Push(command);

        // Исправлено: используем правильный метод для ограничения размера стека
        if (redoStack.Count > maxStackSize)
        {
            // Создаем новый стек с ограниченным количеством элементов
            var limitedItems = redoStack.Take(maxStackSize).ToList();
            redoStack = new Stack<ICommand>(limitedItems);
        }
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;

        var command = redoStack.Pop();
        command.Redo();
        undoStack.Push(command);

        // Исправлено: используем правильный метод для ограничения размера стека
        if (undoStack.Count > maxStackSize)
        {
            var limitedItems = undoStack.Take(maxStackSize).ToList();
            undoStack = new Stack<ICommand>(limitedItems);
        }
    }

    public void ClearStacks()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    private void AddToUndoStack(ICommand command)
    {
        undoStack.Push(command);

        // Исправлено: используем правильный метод для ограничения размера стека
        if (undoStack.Count > maxStackSize)
        {
            var limitedItems = undoStack.Skip(undoStack.Count - maxStackSize).ToList();
            undoStack = new Stack<ICommand>(limitedItems);
        }
    }

    private void ClearRedoStack()
    {
        redoStack.Clear();
    }
}