using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Window for searching and creating nodes in the dialogue graph
/// </summary>
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView graphView;
    private EditorWindow editorWindow;
    private Texture2D indentationIcon;

    // Mapping for human-readable node type names
    private Dictionary<string, string> _nodeTypeDisplayNames = new Dictionary<string, string> {
        {"SpeechNodeText", "Speech (Text)"},
        {"SpeechNodeAudio", "Speech (Audio)"},
        {"SpeechNodeImage", "Speech (Image)"},
        {"SpeechNodeRandText", "Speech Rand (Text)"},
        {"OptionNodeText", "Option (Text)"},
        {"OptionNodeAudio", "Option (Audio)"},
        {"OptionNodeImage", "Option (Image)"},
        {"IntConditionNode", "Condition (Int)"},
        {"StringConditionNode", "Condition (String)"},
        {"CharacterIntConditionNode", "Character Condition (Int)"},
        {"EntryNode", "Entry Node"},
        {"EndNode", "End Node"},
        {"NoteNode", "Note Node"},
        {"TimerNode", "Timer"},
        {"PauseNode", "Pause"},
        {"RandomBranchNode", "Random Branch"},
        {"WireNode", "Wire"},
        {"ModifyIntNode", "Modify Int"},
        {"CharacterModifyIntNode", "Character Modify Int"},
        {"EventNode", "Event"},
        {"DebugLogNode", "Debug Log"},
        {"DebugWarningNode", "Debug Warning"},
        {"DebugErrorNode", "Debug Error"}
    };

    /// <summary>
    /// Initializes the search window
    /// </summary>
    public void Init(EditorWindow editorWindow, DialogueGraphView graphView)
    {
        this.graphView = graphView;
        this.editorWindow = editorWindow;
        // Create a transparent icon for indentation
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        indentationIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        // Get favorite node types from settings
        var settings = LoadDialogueSettings();
        List<string> favoriteNodeTypes = settings?.FavoriteNodeTypes ?? new List<string>();

        // Default favorite node types if settings are null or empty
        if (favoriteNodeTypes.Count == 0)
        {
            favoriteNodeTypes = new List<string> {
                "SpeechNodeText",
                "OptionNodeText",
                "IntConditionNode",
                "EndNode"
            };
        }

        var entries = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
        };

        // Add favorite nodes section if there are favorites
        if (favoriteNodeTypes.Count > 0)
        {
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Favorite Nodes"), 1));

            foreach (var nodeType in favoriteNodeTypes)
            {
                // Skip if node type not in our mapping
                if (!_nodeTypeDisplayNames.ContainsKey(nodeType)) continue;

                entries.Add(new SearchTreeEntry(new GUIContent(_nodeTypeDisplayNames[nodeType], indentationIcon))
                {
                    userData = Type.GetType($"DialogueSystem.{nodeType}") ?? Type.GetType(nodeType),
                    level = 2
                });
            }

            // Add separator between favorites and other nodes
            entries.Add(new SearchTreeEntry(new GUIContent("────────────────────")) { level = 1, userData = null });

        }

        // Group remaining nodes by category
        entries.Add(new SearchTreeGroupEntry(new GUIContent("Speech Nodes"), 1));
        AddNodeEntry(entries, "SpeechNodeText", 2);
        AddNodeEntry(entries, "SpeechNodeAudio", 2);
        AddNodeEntry(entries, "SpeechNodeImage", 2);
        AddNodeEntry(entries, "SpeechNodeRandText", 2);

        entries.Add(new SearchTreeGroupEntry(new GUIContent("Option Nodes"), 1));
        AddNodeEntry(entries, "OptionNodeText", 2);
        AddNodeEntry(entries, "OptionNodeAudio", 2);
        AddNodeEntry(entries, "OptionNodeImage", 2);

        entries.Add(new SearchTreeGroupEntry(new GUIContent("Condition Nodes"), 1));
        AddNodeEntry(entries, "IntConditionNode", 2);
        AddNodeEntry(entries, "StringConditionNode", 2);
        AddNodeEntry(entries, "CharacterIntConditionNode", 2);

        entries.Add(new SearchTreeGroupEntry(new GUIContent("Utility Nodes"), 1));
        AddNodeEntry(entries, "EntryNode", 2);
        AddNodeEntry(entries, "EndNode", 2);
        AddNodeEntry(entries, "NoteNode", 2);
        AddNodeEntry(entries, "TimerNode", 2);
        AddNodeEntry(entries, "PauseNode", 2);
        AddNodeEntry(entries, "RandomBranchNode", 2);
        AddNodeEntry(entries, "WireNode", 2);

        entries.Add(new SearchTreeGroupEntry(new GUIContent("Action Nodes"), 1));
        AddNodeEntry(entries, "ModifyIntNode", 2);
        AddNodeEntry(entries, "CharacterModifyIntNode", 2);
        AddNodeEntry(entries, "EventNode", 2);

        entries.Add(new SearchTreeGroupEntry(new GUIContent("Character Nodes"), 1));
        AddNodeEntry(entries, "CharacterButtonPressNode", 2);

        entries.Add(new SearchTreeGroupEntry(new GUIContent("Debug Nodes"), 1));
        AddNodeEntry(entries, "DebugLogNode", 2);
        AddNodeEntry(entries, "DebugWarningNode", 2);
        AddNodeEntry(entries, "DebugErrorNode", 2);


        return entries;
    }

    /// <summary>
    /// Helper method to add a node entry to the search tree if it's not a favorite
    /// </summary>
    private void AddNodeEntry(List<SearchTreeEntry> entries, string nodeType, int level)
    {
        var settings = LoadDialogueSettings();
        List<string> favoriteNodeTypes = settings?.FavoriteNodeTypes ?? new List<string>();

        // Skip if this node type is already in favorites
        if (favoriteNodeTypes.Contains(nodeType)) return;

        // Get the display name for this node type
        string displayName = _nodeTypeDisplayNames.TryGetValue(nodeType, out string name) ? name : nodeType;

        entries.Add(new SearchTreeEntry(new GUIContent(displayName, indentationIcon))
        {
            userData = Type.GetType($"DialogueSystem.{nodeType}") ?? Type.GetType(nodeType),
            level = level
        });
    }

    /// <summary>
    /// Handles selection of an entry in the search tree
    /// </summary>
    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        // Get the mouse position in local coordinates
        var worldMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
            editorWindow.rootVisualElement.parent,
            context.screenMousePosition - editorWindow.position.position
        );
        var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        // Create the selected node type
        if (searchTreeEntry.userData is Type nodeType)
        {
            // Check for attempting to create a second EntryNode
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

    /// <summary>
    /// Loads the dialogue settings
    /// </summary>
    private DialogueSettingsData LoadDialogueSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueSettingsData");
        if (guids.Length == 0)
            return null;

        // Take the first found settings file
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<DialogueSettingsData>(path);
    }
}