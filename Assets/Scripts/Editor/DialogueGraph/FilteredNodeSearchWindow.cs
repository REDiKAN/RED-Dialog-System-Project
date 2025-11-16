using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class FilteredNodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private EditorWindow _editorWindow;
    private DialogueGraphView _graphView;
    private BaseNode _sourceNode;
    private Action<Type> _onSelect;

    public void Init(EditorWindow editorWindow, DialogueGraphView graphView, BaseNode sourceNode, Action<Type> onSelect)
    {
        _editorWindow = editorWindow;
        _graphView = graphView;
        _sourceNode = sourceNode;
        _onSelect = onSelect;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var allowedTypes = GetAllowedNodeTypes(_sourceNode);
        var entries = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
        };

        // Вручную формируем группы и фильтруем по allowedTypes
        AddGroupIfHasEntries(entries, "Speech Nodes", 1, new[]
        {
            (typeof(SpeechNodeText), "Speech (Text)"),
            (typeof(SpeechNodeAudio), "Speech (Audio)"),
            (typeof(SpeechNodeImage), "Speech (Image)"),
            (typeof(SpeechNodeRandText), "Speech Rand (Text)")
        }, allowedTypes);

        AddGroupIfHasEntries(entries, "Option Nodes", 1, new[]
        {
            (typeof(OptionNodeText), "Option (Text)"),
            (typeof(OptionNodeAudio), "Option (Audio)"),
            (typeof(OptionNodeImage), "Option (Image)")
        }, allowedTypes);

        AddGroupIfHasEntries(entries, "Condition Nodes", 1, new[]
        {
            (typeof(IntConditionNode), "Condition (Int)"),
            (typeof(StringConditionNode), "Condition (String)"),
            (typeof(CharacterIntConditionNode), "Character Condition (Int)")
        }, allowedTypes);

        AddGroupIfHasEntries(entries, "Utility Nodes", 1, new[]
        {
            (typeof(EndNode), "End Node"),
            (typeof(NoteNode), "Note Node"),
            (typeof(TimerNode), "Timer"),
            (typeof(PauseNode), "Pause"),
            (typeof(RandomBranchNode), "Random Branch"),
            (typeof(WireNode), "Wire")
        }, allowedTypes);

        AddGroupIfHasEntries(entries, "Action Nodes", 1, new[]
        {
            (typeof(ModifyIntNode), "Modify Int"),
            (typeof(CharacterModifyIntNode), "Character Modify Int"),
            (typeof(EventNode), "Event")
        }, allowedTypes);

        AddGroupIfHasEntries(entries, "Debug Nodes", 1, new[]
        {
            (typeof(DebugLogNode), "Debug Log"),
            (typeof(DebugWarningNode), "Debug Warning"),
            (typeof(DebugErrorNode), "Debug Error")
        }, allowedTypes);

        return entries;
    }

    private void AddGroupIfHasEntries(
        List<SearchTreeEntry> entries,
        string groupName,
        int level,
        (Type type, string label)[] candidates,
        HashSet<Type> allowed)
    {
        var valid = candidates.Where(c => allowed.Contains(c.type)).ToArray();
        if (valid.Length == 0) return;

        entries.Add(new SearchTreeGroupEntry(new GUIContent(groupName), level));
        foreach (var (type, label) in valid)
        {
            entries.Add(new SearchTreeEntry(new GUIContent(label))
            {
                level = level + 1,
                userData = type
            });
        }
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if (entry.userData is Type nodeType)
        {
            _onSelect?.Invoke(nodeType);
            return true;
        }
        return false;
    }

    private HashSet<Type> GetAllowedNodeTypes(BaseNode source)
    {
        var allowed = new HashSet<Type>();

        // Speech → Speech, Option, Condition, End, Modify, Event, Timer, Pause, RandomBranch
        if (source is SpeechNodeText or SpeechNodeAudio or SpeechNodeImage or SpeechNodeRandText)
        {
            allowed.UnionWith(new[]
            {
        typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText), // Добавлено
        typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
        typeof(IntConditionNode), typeof(StringConditionNode), typeof(CharacterIntConditionNode),
        typeof(EndNode), typeof(EventNode), typeof(TimerNode), typeof(PauseNode),
        typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
        typeof(WireNode), typeof(RandomBranchNode)
            });
        }
        // Option → Speech, Condition, End, Modify, RandomBranch
        else if (source is OptionNodeText or OptionNodeAudio or OptionNodeImage)
        {
            allowed.UnionWith(new[]
            {
                typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
                typeof(IntConditionNode), typeof(StringConditionNode), typeof(CharacterIntConditionNode),
                typeof(EndNode), typeof(EventNode),
                typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
                typeof(WireNode), typeof(RandomBranchNode)
            });
        }
        // Condition/Modify/Event/Timer/Pause/Wire/RandomBranch → почти всё (кроме Entry)
        else if (source is BaseConditionNode or
                 ModifyIntNode or CharacterModifyIntNode or
                 EventNode or TimerNode or PauseNode or
                 WireNode or RandomBranchNode)
        {
            allowed.UnionWith(new[]
            {
                typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
                typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
                typeof(IntConditionNode), typeof(StringConditionNode), typeof(CharacterIntConditionNode),
                typeof(EndNode), typeof(EventNode), typeof(TimerNode), typeof(PauseNode),
                typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
                typeof(WireNode), typeof(RandomBranchNode),
                typeof(DebugLogNode), typeof(DebugWarningNode), typeof(DebugErrorNode)
            });
        }
        // Entry → только Speech
        else if (source is EntryNode)
        {
            allowed.UnionWith(new[]
            {
                typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText)
            });
        }
        // Note — не ограничиваем (но на практике редко используется как источник)
        else if (source is NoteNode)
        {
            // Разрешаем всё, кроме EntryNode
            var allTypes = new[]
            {
                typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
                typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
                typeof(IntConditionNode), typeof(StringConditionNode), typeof(CharacterIntConditionNode),
                typeof(EndNode), typeof(EventNode), typeof(TimerNode), typeof(PauseNode),
                typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
                typeof(WireNode), typeof(RandomBranchNode),
                typeof(DebugLogNode), typeof(DebugWarningNode), typeof(DebugErrorNode)
            };
            allowed.UnionWith(allTypes);
        }

        return allowed;
    }
}