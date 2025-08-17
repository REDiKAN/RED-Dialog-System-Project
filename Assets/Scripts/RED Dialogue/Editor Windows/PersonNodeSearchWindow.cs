using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// ���� ������ ����� ��� Person �����
/// </summary>
public class PersonNodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private PersonGraphView graphView;
    private EditorWindow window;
    private Texture2D indentationIcon;

    /// <summary>
    /// ������������� ���� ������
    /// </summary>
    public void Init(EditorWindow _window, PersonGraphView _graphView)
    {
        graphView = _graphView;
        window = _window;

        // ������� ���������� ������ ��� ��������
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        indentationIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        throw new System.NotImplementedException();
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        throw new System.NotImplementedException();
    }
}
