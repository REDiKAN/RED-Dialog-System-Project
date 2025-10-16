// Assets/Scripts/Editor/DialogueGraph/Utilities/NodeFactory.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public static class NodeFactory
{
    public static BaseNode CreateNode(Type nodeType, Vector2 position)
    {
        if (nodeType == typeof(EntryNode))
            return CreateEntryNode(position);
        else if (nodeType == typeof(SpeechNode))
            return CreateSpeechNode(position);
        else if (nodeType == typeof(SpeechNodeText))
            return CreateSpeechNodeText(position);
        else if (nodeType == typeof(SpeechNodeAudio))
            return CreateSpeechNodeAudio(position);
        else if (nodeType == typeof(SpeechNodeImage))
            return CreateSpeechNodeImage(position);
        else if (nodeType == typeof(SpeechNodeRandText))
            return CreateSpeechNodeRandText(position);
        else if (nodeType == typeof(OptionNode))
            return CreateOptionNode(position);
        else if (nodeType == typeof(OptionNodeText))
            return CreateOptionNodeText(position);
        else if (nodeType == typeof(OptionNodeAudio))
            return CreateOptionNodeAudio(position);
        else if (nodeType == typeof(OptionNodeImage))
            return CreateOptionNodeImage(position);
        else if (nodeType == typeof(IntConditionNode))
            return CreateIntConditionNode(position);
        else if (nodeType == typeof(StringConditionNode))
            return CreateStringConditionNode(position);
        else if (nodeType == typeof(ModifyIntNode))
            return CreateModifyIntNode(position);
        else if (nodeType == typeof(EndNode))
            return CreateEndNode(position);
        else if (nodeType == typeof(EventNode))
            return CreateEventNode(position);
        else if (nodeType == typeof(CharacterIntConditionNode))
            return CreateCharacterIntConditionNode(position);
        else if (nodeType == typeof(CharacterModifyIntNode))
            return CreateCharacterModifyIntNode(position);
        else if (nodeType == typeof(DebugLogNode))
            return CreateDebugLogNode(position);
        else if (nodeType == typeof(DebugWarningNode))
            return CreateDebugWarningNode(position);
        else if (nodeType == typeof(DebugErrorNode))
            return CreateDebugErrorNode(position);
        else if (nodeType == typeof(RandomBranchNode))
            return CreateRandomBranchNode(position);
        if (nodeType == typeof(NoteNode))
            return CreateNoteNode(position);
        else
        {
            Debug.LogError($"NodeFactory: Unknown node type {nodeType}");
            return null;
        }
    }

    public static BaseNode CreateNoteNode(Vector2 position)
    {
        var node = new NoteNode();
        node.Initialize(position);
        node.SetPosition(new Rect(position, new Vector2(300, 150)));
        return node;
    }


    public static EntryNode CreateEntryNode(Vector2 position)
    {
        var node = new EntryNode();
        node.Initialize(position);
        return node;
    }

    public static SpeechNode CreateSpeechNode(Vector2 position)
    {
        var node = new SpeechNode();
        node.Initialize(position);
        return node;
    }

    public static SpeechNodeText CreateSpeechNodeText(Vector2 position)
    {
        var node = new SpeechNodeText();
        node.Initialize(position);
        return node;
    }

    public static SpeechNodeAudio CreateSpeechNodeAudio(Vector2 position)
    {
        var node = new SpeechNodeAudio();
        node.Initialize(position);
        return node;
    }

    public static SpeechNodeImage CreateSpeechNodeImage(Vector2 position)
    {
        var node = new SpeechNodeImage();
        node.Initialize(position);
        return node;
    }

    public static SpeechNodeRandText CreateSpeechNodeRandText(Vector2 position)
    {
        var node = new SpeechNodeRandText();
        node.Initialize(position);
        return node;
    }

    public static OptionNode CreateOptionNode(Vector2 position)
    {
        var node = new OptionNode();
        node.Initialize(position);
        return node;
    }

    public static OptionNodeText CreateOptionNodeText(Vector2 position)
    {
        var node = new OptionNodeText();
        node.Initialize(position);
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

    public static EndNode CreateEndNode(Vector2 position)
    {
        var node = new EndNode();
        node.Initialize(position);
        return node;
    }

    public static EventNode CreateEventNode(Vector2 position)
    {
        var node = new EventNode();
        node.Initialize(position);
        return node;
    }

    public static CharacterIntConditionNode CreateCharacterIntConditionNode(Vector2 position)
    {
        var node = new CharacterIntConditionNode();
        node.Initialize(position);
        return node;
    }

    public static CharacterModifyIntNode CreateCharacterModifyIntNode(Vector2 position)
    {
        var node = new CharacterModifyIntNode();
        node.Initialize(position);
        return node;
    }

    public static DebugLogNode CreateDebugLogNode(Vector2 position)
    {
        var node = new DebugLogNode();
        node.Initialize(position);
        return node;
    }

    public static DebugWarningNode CreateDebugWarningNode(Vector2 position)
    {
        var node = new DebugWarningNode();
        node.Initialize(position);
        return node;
    }

    public static DebugErrorNode CreateDebugErrorNode(Vector2 position)
    {
        var node = new DebugErrorNode();
        node.Initialize(position);
        return node;
    }

    public static RandomBranchNode CreateRandomBranchNode(Vector2 position)
    {
        var node = new RandomBranchNode();
        node.Initialize(position);
        return node;
    }
}