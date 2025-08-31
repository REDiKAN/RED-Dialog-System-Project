using UnityEngine;

/// <summary>
/// ������� ��� �������� ����� ����������� �����
/// ������������ ������ �������� ���� ����� �����
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// ������� ���� ���������� ���� � �������� �������
    /// </summary>
    public static BaseNode CreateNode(System.Type nodeType, Vector2 position)
    {
        return nodeType.Name switch
        {
            nameof(SpeechNode) => CreateSpeechNode(position),
            nameof(OptionNode) => CreateOptionNode(position),
            nameof(EntryNode) => CreateEntryNode(position),
            nameof(IntConditionNode) => CreateIntConditionNode(position),
            nameof(StringConditionNode) => CreateStringConditionNode(position),
            _ => null
        };
    }

    public static IntConditionNode CreateIntConditionNode(Vector2 position)
    {
        var node = new IntConditionNode();
        node.Initialize(position);
        return node;
    }

    public static StringConditionNode CreateStringConditionNode(Vector2 position)
    {
        var node = new StringConditionNode();
        node.Initialize(position);
        return node;
    }

    /// <summary>
    /// ������� ���� ���� NPC
    /// </summary>
    public static SpeechNode CreateSpeechNode(Vector2 position, string dialogueText = "New Dialogue")
    {
        var node = new SpeechNode();
        node.Initialize(position);
        node.DialogueText = dialogueText;
        return node;
    }

    /// <summary>
    /// ������� ���� �������� ������ ������
    /// </summary>
    public static OptionNode CreateOptionNode(Vector2 position, string responseText = "New Response")
    {
        var node = new OptionNode();
        node.Initialize(position);
        node.ResponseText = responseText;
        return node;
    }

    /// <summary>
    /// ������� ��������� ����
    /// </summary>
    public static EntryNode CreateEntryNode(Vector2 position)
    {
        var node = new EntryNode();
        node.Initialize(position);
        return node;
    }
}