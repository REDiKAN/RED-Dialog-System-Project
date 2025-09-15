using UnityEngine;
using UnityEditor;

/// <summary>
/// ������� ��� �������� ����� ����������� �����
/// ������������ ������ �������� ���� ����� �����
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// ������� ���� ���������� ���� � �������� �������
    /// </summary>
    private static DialogueGraphView GetGraphView()
    {
        // �������� ��� ���� ���� DialogueGraph
        var windows = Resources.FindObjectsOfTypeAll<DialogueGraph>();
        if (windows.Length > 0)
        {
            return windows[0].graphView;
        }
        return null;
    }

    /// <summary>
    /// ������� ���� ���������� ���� � �������� �������
    /// </summary>
    public static BaseNode CreateNode(System.Type nodeType, Vector2 position)
    {
        return nodeType.Name switch
        {
            nameof(SpeechNode) => CreateSpeechNode(position),
            nameof(SpeechNodeText) => CreateSpeechNodeText(position),
            nameof(SpeechNodeAudio) => CreateSpeechNodeAudio(position),
            nameof(SpeechNodeImage) => CreateSpeechNodeImage(position),
            nameof(OptionNode) => CreateOptionNode(position),
            nameof(OptionNodeText) => CreateOptionNodeText(position),
            nameof(OptionNodeAudio) => CreateOptionNodeAudio(position),
            nameof(OptionNodeImage) => CreateOptionNodeImage(position),
            nameof(EntryNode) => CreateEntryNode(position),
            nameof(IntConditionNode) => CreateIntConditionNode(position),
            nameof(StringConditionNode) => CreateStringConditionNode(position),
            nameof(ModifyIntNode) => CreateModifyIntNode(position),
            nameof(EndNode) => CreateEndNode(position),
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

    public static ModifyIntNode CreateModifyIntNode(Vector2 position)
    {
        var node = new ModifyIntNode();
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

        // �������������� ��������� �������� ���������
        var baseCharacter = GetBaseCharacter();
        if (baseCharacter != null)
        {
            node.SetSpeaker(baseCharacter);
        }

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

    public static EndNode CreateEndNode(Vector2 position, string nextDialogueName = "")
    {
        var node = new EndNode();
        node.Initialize(position);
        node.NextDialogueName = nextDialogueName;
        return node;
    }

    public static SpeechNodeText CreateSpeechNodeText(Vector2 position, string dialogueText = "New Dialogue")
    {
        var node = new SpeechNodeText();
        node.Initialize(position);
        node.DialogueText = dialogueText;

        var baseCharacter = GetBaseCharacter();
        if (baseCharacter != null)
        {
            node.SetSpeaker(baseCharacter);
        }

        return node;
    }

    public static SpeechNodeAudio CreateSpeechNodeAudio(Vector2 position)
    {
        var node = new SpeechNodeAudio();
        node.Initialize(position);

        var baseCharacter = GetBaseCharacter();
        if (baseCharacter != null)
        {
            node.SetSpeaker(baseCharacter);
        }

        return node;
    }

    public static SpeechNodeImage CreateSpeechNodeImage(Vector2 position)
    {
        var node = new SpeechNodeImage();
        node.Initialize(position);

        var baseCharacter = GetBaseCharacter();
        if (baseCharacter != null)
        {
            node.SetSpeaker(baseCharacter);
        }

        return node;
    }

    public static OptionNodeText CreateOptionNodeText(Vector2 position, string responseText = "New Response")
    {
        var node = new OptionNodeText();
        node.Initialize(position);
        node.ResponseText = responseText;
        return node;
    }

    public static OptionNodeAudio CreateOptionNodeAudio(Vector2 position)
    {
        var node = new OptionNodeAudio();
        node.Initialize(position);
        return node;
    }

    public static OptionNodeImage CreateOptionNodeImage(Vector2 position)
    {
        var node = new OptionNodeImage();
        node.Initialize(position);
        return node;
    }

    private static CharacterData GetBaseCharacter()
    {
        var graphView = GetGraphView();
        if (graphView != null && !string.IsNullOrEmpty(graphView.BaseCharacterGuid))
        {
            return AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(graphView.BaseCharacterGuid);
        }
        return null;
    }

}