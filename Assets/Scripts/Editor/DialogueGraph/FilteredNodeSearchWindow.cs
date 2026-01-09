using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

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
        List<SearchTreeEntry> tree = new List<SearchTreeEntry>();

        // Корневая папка
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Dialogue Nodes"), 0));

        // ===== Group 1: Flow Control (Управление потоком) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Flow Control"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Entry Node")) { level = 2, userData = typeof(EntryNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("End Node")) { level = 2, userData = typeof(EndNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Wire Node")) { level = 2, userData = typeof(WireNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Random Branch Node")) { level = 2, userData = typeof(RandomBranchNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Pause Node")) { level = 2, userData = typeof(PauseNode) });

        // ===== Group 2: Dialogue Content (Контент диалога) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Dialogue Content"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Speech Node")) { level = 2, userData = typeof(SpeechNodeText) });
        tree.Add(new SearchTreeEntry(new GUIContent("Speech Image Node")) { level = 2, userData = typeof(SpeechNodeImage) });
        tree.Add(new SearchTreeEntry(new GUIContent("Speech Random Node")) { level = 2, userData = typeof(SpeechNodeRandText) });

        // ===== Group 3: Player Choice (Выбор игрока) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Player Choice"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Option Node")) { level = 2, userData = typeof(OptionNodeText) });
        tree.Add(new SearchTreeEntry(new GUIContent("Option Image Node")) { level = 2, userData = typeof(OptionNodeImage) });
        tree.Add(new SearchTreeEntry(new GUIContent("Timer Node")) { level = 2, userData = typeof(TimerNode) });

        // ===== Group 4: Conditions & Logic (Условия и Логика) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Conditions & Logic"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Int Condition Node")) { level = 2, userData = typeof(IntConditionNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("String Condition Node")) { level = 2, userData = typeof(StringConditionNode) });

        // ===== Group 5: Variables (Переменные) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Variables"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Modify Int Node")) { level = 2, userData = typeof(ModifyIntNode) });

        // ===== Group 6: Characters (Персонажи) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("Characters"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Character Int Condition Node")) { level = 2, userData = typeof(CharacterIntConditionNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Character Modify Int Node")) { level = 2, userData = typeof(CharacterModifyIntNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Character Button Press Node")) { level = 2, userData = typeof(CharacterButtonPressNode) });

        // ===== Group 7: UI & Chat Control (Управление интерфейсом) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("UI & Chat Control"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Chat Switch Node")) { level = 2, userData = typeof(ChatSwitchNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Change Chat Name Node")) { level = 2, userData = typeof(ChangeChatNameNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Change Chat Icon Node")) { level = 2, userData = typeof(ChangeChatIconNode) });

        // ===== Group 8: System & Events (Система и События) =====
        tree.Add(new SearchTreeGroupEntry(new GUIContent("System & Events"), 1));
        tree.Add(new SearchTreeEntry(new GUIContent("Event Node")) { level = 2, userData = typeof(EventNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Debug Log Node")) { level = 2, userData = typeof(DebugLogNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Debug Warning Node")) { level = 2, userData = typeof(DebugWarningNode) });
        tree.Add(new SearchTreeEntry(new GUIContent("Debug Error Node")) { level = 2, userData = typeof(DebugErrorNode) });

        return tree;
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