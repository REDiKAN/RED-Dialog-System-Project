using UnityEngine;

/// <summary>
/// Фабрика для создания узлов диалогового графа
/// Централизует логику создания всех типов узлов
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// Создает узел указанного типа в заданной позиции
    /// </summary>
    public static BaseNode CreateNode(System.Type nodeType, Vector2 position)
    {
        if (nodeType == typeof(SpeechNode))
            return CreateSpeechNode(position);
        else if (nodeType == typeof(OptionNode))
            return CreateOptionNode(position);
        else if (nodeType == typeof(EntryNode))
            return CreateEntryNode(position);

        return null;
    }

    /// <summary>
    /// Создает узел речи NPC
    /// </summary>
    public static SpeechNode CreateSpeechNode(Vector2 position, string dialogueText = "New Dialogue")
    {
        var node = new SpeechNode();
        node.Initialize(position);
        node.DialogueText = dialogueText;
        return node;
    }

    /// <summary>
    /// Создает узел варианта ответа игрока
    /// </summary>
    public static OptionNode CreateOptionNode(Vector2 position, string responseText = "New Response")
    {
        var node = new OptionNode();
        node.Initialize(position);
        node.ResponseText = responseText;
        return node;
    }

    /// <summary>
    /// Создает стартовый узел
    /// </summary>
    public static EntryNode CreateEntryNode(Vector2 position)
    {
        var node = new EntryNode();
        node.Initialize(position);
        return node;
    }
}