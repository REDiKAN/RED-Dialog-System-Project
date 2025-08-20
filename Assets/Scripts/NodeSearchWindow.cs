using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System;

/// <summary>
/// Окно поиска узлов для диалогового графа
/// Позволяет создавать новые узлы через поиск
/// </summary>
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView graphView;
    private EditorWindow editorWindow;
    private Texture2D indentationIcon;

    /// <summary>
    /// Инициализация окна поиска
    /// </summary>
    public void Init(EditorWindow editorWindow, DialogueGraphView graphView)
    {
        this.graphView = graphView;
        this.editorWindow = editorWindow;

        // Создаем прозрачную иконку для отступов
        indentationIcon = new Texture2D(1, 1);
        indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        indentationIcon.Apply();
    }

    /// <summary>
    /// Создает дерево элементов для поиска
    /// </summary>
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue Nodes"), 1),
            new SearchTreeEntry(new GUIContent("Speech Node", indentationIcon))
            {
                userData = typeof(SpeechNode),
                level = 2
            },
            new SearchTreeEntry(new GUIContent("Option Node", indentationIcon))
            {
                userData = typeof(OptionNode),
                level = 2
            },
            new SearchTreeGroupEntry(new GUIContent("Utility Nodes"), 1),
            new SearchTreeEntry(new GUIContent("Entry Node", indentationIcon))
            {
                userData = typeof(EntryNode),
                level = 2
            }
        };

        return tree;
    }

    /// <summary>
    /// Обрабатывает выбор элемента в окне поиска
    /// </summary>
    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        // Конвертируем координаты мыши в локальные координаты графа
        var worldMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
            editorWindow.rootVisualElement.parent,
            context.screenMousePosition - editorWindow.position.position
        );

        var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        // Создаем узел выбранного типа
        if (searchTreeEntry.userData is Type nodeType)
        {
            graphView.CreateNode(nodeType, localMousePosition);
            return true;
        }

        return false;
    }
}