using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// ���� ������ ����� ��� ����������� �����
/// ��������� ��������� ����� ���� ����� �����
/// </summary>
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView graphView;
    private EditorWindow editorWindow;
    private Texture2D indentationIcon;

    /// <summary>
    /// ������������� ���� ������
    /// </summary>
    public void Init(EditorWindow editorWindow, DialogueGraphView graphView)
    {
        this.graphView = graphView;
        this.editorWindow = editorWindow;

        // ������� ���������� ������ ��� ��������
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        indentationIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        return new List<SearchTreeEntry>
    {
        new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
        new SearchTreeGroupEntry(new GUIContent("Dialogue Nodes"), 1),
        new SearchTreeGroupEntry(new GUIContent("Speech Nodes"), 2),
        new SearchTreeEntry(new GUIContent("Speech (Text)", indentationIcon)) { userData = typeof(SpeechNodeText), level = 3 },
        new SearchTreeEntry(new GUIContent("Speech (Audio)", indentationIcon)) { userData = typeof(SpeechNodeAudio), level = 3 },
        new SearchTreeEntry(new GUIContent("Speech (Image)", indentationIcon)) { userData = typeof(SpeechNodeImage), level = 3 },
        new SearchTreeEntry(new GUIContent("Speech Rand (Text)", indentationIcon)) { userData = typeof(SpeechNodeRandText), level = 3 },
        new SearchTreeGroupEntry(new GUIContent("Option Nodes"), 2),
        new SearchTreeEntry(new GUIContent("Option (Text)", indentationIcon)) { userData = typeof(OptionNodeText), level = 3 },
        new SearchTreeEntry(new GUIContent("Option (Audio)", indentationIcon)) { userData = typeof(OptionNodeAudio), level = 3 },
        new SearchTreeEntry(new GUIContent("Option (Image)", indentationIcon)) { userData = typeof(OptionNodeImage), level = 3 },
        new SearchTreeGroupEntry(new GUIContent("Condition Nodes"), 1),
        new SearchTreeEntry(new GUIContent("Condition (Int)", indentationIcon)) { userData = typeof(IntConditionNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Condition (String)", indentationIcon)) { userData = typeof(StringConditionNode), level = 2 },
        new SearchTreeGroupEntry(new GUIContent("Utility Nodes"), 1),
        new SearchTreeEntry(new GUIContent("Entry Node", indentationIcon)) { userData = typeof(EntryNode), level = 2 },
        new SearchTreeEntry(new GUIContent("End Node", indentationIcon)) { userData = typeof(EndNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Note Node", indentationIcon)) { userData = typeof(NoteNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Random Branch", indentationIcon)) { userData = typeof(RandomBranchNode), level = 2 },
        new SearchTreeGroupEntry(new GUIContent("Action Nodes"), 1),
        new SearchTreeEntry(new GUIContent("Modify Int", indentationIcon)) { userData = typeof(ModifyIntNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Event", indentationIcon)) { userData = typeof(EventNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Character Condition (Int)", indentationIcon)) { userData = typeof(CharacterIntConditionNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Character Modify Int", indentationIcon)) { userData = typeof(CharacterModifyIntNode), level = 2 },
        new SearchTreeGroupEntry(new GUIContent("Debug Nodes"), 1),
        new SearchTreeEntry(new GUIContent("Debug Log", indentationIcon)) { userData = typeof(DebugLogNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Debug Warning", indentationIcon)) { userData = typeof(DebugWarningNode), level = 2 },
        new SearchTreeEntry(new GUIContent("Debug Error", indentationIcon)) { userData = typeof(DebugErrorNode), level = 2 },
    };
    }

    /// <summary>
    /// ������������ ����� �������� � ���� ������
    /// </summary>
    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        // ������������ ���������� ���� � ��������� ���������� �����
        var worldMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
            editorWindow.rootVisualElement.parent,
            context.screenMousePosition - editorWindow.position.position
        );

        var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        // ������� ���� ���������� ����
        if (searchTreeEntry.userData is Type nodeType)
        {
            // Проверка на попытку создания второго EntryNode
            if (nodeType == typeof(EntryNode))
            {
                var existingEntryNodes = graphView.nodes.ToList().Where(node => node is EntryNode);
                if (existingEntryNodes.Any())
                {
                    EditorUtility.DisplayDialog("Cannot Create Start Node",
                        "Only one Start Node is allowed in the graph.", "OK");
                    return false;
                }
            }

            graphView.CreateNode(nodeType, localMousePosition);
            return true;
        }

        return false;
    }
}