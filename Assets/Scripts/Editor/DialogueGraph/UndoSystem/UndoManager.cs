using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер для управления операциями отмены действий в графе диалога
/// </summary>
public class UndoManager
{
    private readonly DialogueGraphView graphView;
    private readonly Stack<ICommand> undoStack = new Stack<ICommand>();
    private bool isUndoing = false;

    public UndoManager(DialogueGraphView graphView)
    {
        this.graphView = graphView;
    }

    /// <summary>
    /// Выполняет команду и добавляет её в стек для отмены
    /// </summary>
    public void ExecuteCommand(ICommand command)
    {
        if (isUndoing)
            return;

        // Выполняем команду
        command.Execute();

        // Добавляем в стек отмены
        undoStack.Push(command);

        // Отмечаем изменения в графе
        graphView.MarkUnsavedChangeWithoutFile();
    }

    /// <summary>
    /// Отменяет последнюю команду
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count == 0)
            return;

        isUndoing = true;

        // Достаём команду из стека отмены
        ICommand command = undoStack.Pop();

        // Выполняем отмену
        command.Undo();

        // Отмечаем изменения в графе
        graphView.MarkUnsavedChangeWithoutFile();

        isUndoing = false;
    }

    /// <summary>
    /// Очищает стек отмены
    /// </summary>
    public void ClearStacks()
    {
        undoStack.Clear();
    }

    /// <summary>
    /// Проверяет, можно ли выполнить отмену
    /// </summary>
    public bool CanUndo()
    {
        return undoStack.Count > 0;
    }
}