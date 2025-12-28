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
            (typeof(WireNode), "Wire"),
            (typeof(ChangeChatIconNode), "ChangeChatIcon"),
            (typeof(ChangeChatNameNode), "ChangeChatName")

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

        // Определяем тип исходного узла
        Type sourceType = source.GetType();

        // Speech Node может соединяться со следующими типами
        if (sourceType == typeof(SpeechNodeText) ||
            sourceType == typeof(SpeechNodeAudio) ||
            sourceType == typeof(SpeechNodeImage) ||
            sourceType == typeof(SpeechNodeRandText))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Option Node может соединяться со следующими типами
        else if (sourceType == typeof(OptionNodeText) ||
                 sourceType == typeof(OptionNodeAudio) ||
                 sourceType == typeof(OptionNodeImage))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Condition Node может соединяться со следующими типами
        else if (sourceType == typeof(IntConditionNode) ||
                 sourceType == typeof(StringConditionNode) ||
                 sourceType == typeof(CharacterIntConditionNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Modify Node может соединяться со следующими типами
        else if (sourceType == typeof(ModifyIntNode) ||
                 sourceType == typeof(CharacterModifyIntNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Timer Node может соединяться со следующими типами
        else if (sourceType == typeof(TimerNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Pause Node может соединяться со следующими типами
        else if (sourceType == typeof(PauseNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // RandomBranch Node может соединяться со следующими типами
        else if (sourceType == typeof(RandomBranchNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(WireNode)
        });
        }
        // Wire Node может соединяться со следующими типами
        else if (sourceType == typeof(WireNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode)
        });
        }
        // Entry Node может соединяться только с Speech Node
        else if (sourceType == typeof(EntryNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText)
        });
        }
        // End Node не может соединяться ни с чем
        else if (sourceType == typeof(EndNode))
        {
            // Ничего не добавляем
        }
        // Event Node может соединяться со следующими типами
        else if (sourceType == typeof(EventNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Note Node может соединяться со всеми типами
        else if (sourceType == typeof(NoteNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        // Debug Nodes могут соединяться со всеми типами
        else if (sourceType == typeof(DebugLogNode) ||
                 sourceType == typeof(DebugWarningNode) ||
                 sourceType == typeof(DebugErrorNode))
        {
            allowed.UnionWith(new Type[] {
            typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
            typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
            typeof(IntConditionNode), typeof(StringConditionNode),
            typeof(CharacterIntConditionNode),
            typeof(EndNode), typeof(EventNode),
            typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
            typeof(TimerNode), typeof(PauseNode),
            typeof(RandomBranchNode), typeof(WireNode)
        });
        }
        else if (sourceType == typeof(CharacterButtonPressNode))
        {
            allowed.UnionWith(new Type[] {
        typeof(SpeechNodeText), typeof(SpeechNodeAudio), typeof(SpeechNodeImage), typeof(SpeechNodeRandText),
        typeof(OptionNodeText), typeof(OptionNodeAudio), typeof(OptionNodeImage),
        typeof(IntConditionNode), typeof(StringConditionNode),
        typeof(CharacterIntConditionNode),
        typeof(EndNode), typeof(EventNode),
        typeof(ModifyIntNode), typeof(CharacterModifyIntNode),
        typeof(TimerNode), typeof(PauseNode),
        typeof(RandomBranchNode), typeof(WireNode),
        typeof(CharacterButtonPressNode)
    });
        }

        return allowed;
    }
}