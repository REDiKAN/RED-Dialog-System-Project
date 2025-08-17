using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ���� ������ ����� ��� ����������� �����
/// </summary>
public class DialogueNodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView graphView;
    private EditorWindow window;
    private Texture2D indentationIcon;

    /// <summary>
    /// ������������� ���� ������
    /// </summary>
    public void Init(EditorWindow _window, DialogueGraphView _graphView)
    {
        graphView = _graphView;
        window = _window;

        // ������� ���������� ������ ��� ��������
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        indentationIcon.Apply();
    }

    #region Search Tree Creation
    /// <summary>
    /// �������� ������ ��������� ��� ������
    /// </summary>
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
            new SearchTreeEntry(new GUIContent("Option Node", indentationIcon))
            {
                userData = new OptionNode(),
                level = 2
            },
            new SearchTreeEntry(new GUIContent("Speech Node", indentationIcon))
            {
                userData = new SpeechNode(),
                level = 2
            },
            new SearchTreeEntry(new GUIContent("Flow Node", indentationIcon))
            {
                userData = new FlowNode(),
                level = 2
            }
        };
        return tree;
    }
    #endregion

    #region Node Selection
    /// <summary>
    /// ��������� ������ �������� � ���� ������
    /// </summary>
    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        // ����������� ��������� ���� � ��������� ���������� �����
        var worldMousePosition = window.rootVisualElement.ChangeCoordinatesTo(
            window.rootVisualElement.parent,
            context.screenMousePosition - window.position.position
        );
        var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        // ��������� �������� �����
        switch (SearchTreeEntry.userData)
        {
            case SpeechNode speechNode:
                graphView.CreateNode(speechNode, "Speech Node", localMousePosition);
                return true;
            case OptionNode optionNode:
                graphView.CreateNode(optionNode, "Option Node", localMousePosition);
                return true;
            case FlowNode flowNode:
                graphView.CreateNode(flowNode, "Flow Node", localMousePosition);
                return true;
            default:
                return false;
        }
    }
    #endregion
}