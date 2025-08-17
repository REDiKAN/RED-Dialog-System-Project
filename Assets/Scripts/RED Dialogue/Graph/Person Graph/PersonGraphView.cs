using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class PersonGraphView : GraphView
{
    public readonly Vector2 defaultNodeSize = new Vector2(500, 500);
    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();

    public PersonGraphView(EditorWindow editorWindow)
    {
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // Добавляем манипуляторы
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Добавляем сетку
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddElement(GenerateEntryPointNode());
        //AddSearchWindow(editorWindow);
    }

    /// <summary>
    /// Создание стартового узла
    /// </summary>
    private PersonNode GenerateEntryPointNode()
    {
        var node = new PersonNode
        {
            title = "Person Data",
            GUID = Guid.NewGuid().ToString(),
            PersonName = new PersonName
            {
                GivenName = "Given Name",
                FamilyName = "Family Name",
                Patronymic = "Patronymic"
            },
            Description = "non description !"
        };

        // Генерируем выходной порт
        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "PersonName";
        node.outputContainer.Add(generatedPort);

        // Запрещаем перемещение и удаление
        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        // Применяем стиль
        node.styleSheets.Add(Resources.Load<StyleSheet>("StartNode"));
        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(100, 200, 100, 150));

        return node;
    }

    private Port GeneratePort(PersonNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }
}
