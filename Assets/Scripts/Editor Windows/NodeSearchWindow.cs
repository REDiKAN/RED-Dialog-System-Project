using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно поиска узлов для диалогового графа
/// </summary>
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView graphView;
    private EditorWindow window;
    private Texture2D indentationIcon;

    /// <summary>
    /// Инициализация окна поиска
    /// </summary>
    public void Init(EditorWindow _window, DialogueGraphView _graphView)
    {
        graphView = _graphView;
        window = _window;

        // Создаем прозрачную иконку для отступов
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        indentationIcon.Apply();
    }

    #region Search Tree Creation
    /// <summary>
    /// Создание дерева элементов для поиска
    /// </summary>
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
            new SearchTreeEntry(new GUIContent("Dialogue Node", indentationIcon))
            {
                userData = new DialogueNode(),
                level = 2
            }
        };
        return tree;
    }
    #endregion

    #region Node Selection
    /// <summary>
    /// Обработка выбора элемента в окне поиска
    /// </summary>
    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        // Конвертация координат мыши в локальные координаты графа
        var worldMousePosition = window.rootVisualElement.ChangeCoordinatesTo(
            window.rootVisualElement.parent,
            context.screenMousePosition - window.position.position
        );
        var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        // Обработка создания узлов
        switch (SearchTreeEntry.userData)
        {
            case DialogueNode dialogueNode:
                graphView.CreateNode("Dialogue Node", localMousePosition);
                return true;
            default:
                return false;
        }
    }
    #endregion
}