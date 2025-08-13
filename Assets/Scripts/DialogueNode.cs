using UnityEditor.Experimental.GraphView;

/// <summary>
/// Узел диалогового графа
/// </summary>
public class DialogueNode : Node
{
    /// <summary>Уникальный идентификатор узла</summary>
    public string GUID;

    /// <summary>Текст диалога</summary>
    public string DialogueText;

    /// <summary>Флаг стартового узла</summary>
    public bool EntryPoint;
}
