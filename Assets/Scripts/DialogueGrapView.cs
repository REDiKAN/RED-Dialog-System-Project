using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

/// <summary>
/// Граф для редактирования диалогов
/// </summary>
public class DialogueGraphView : GraphView
{
    public readonly Vector2 defaultNodeSize = new Vector2(150, 200);
    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
    private NodeSearchWindow searchWindow;

    #region Initialization
    public DialogueGraphView(EditorWindow editorWindow)
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
        AddSearchWindow(editorWindow);
    }

    private void AddSearchWindow(EditorWindow editorWindow)
    {
        searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        searchWindow.Init(editorWindow, this);
        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }
    #endregion

    #region Port Operations
    /// <summary>
    /// Получение совместимых портов для соединения
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(port =>
            port != startPort &&
            port.node != startPort.node &&
            port.direction != startPort.direction).ToList();
    }

    private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }
    #endregion

    #region Node Creation
    /// <summary>
    /// Создание и добавление узла в граф
    /// </summary>
    public void CreateNode(string nodeName, Vector2 mousePosition)
    {
        AddElement(CreateDialogueNode(nodeName, mousePosition));
    }

    /// <summary>
    /// Создание стартового узла
    /// </summary>
    private DialogueNode GenerateEntryPointNode()
    {
        var node = new DialogueNode
        {
            title = "START",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "ENTRY POINT",
            EntryPoint = true
        };

        // Генерируем выходной порт
        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";
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

    /// <summary>
    /// Создание диалогового узла
    /// </summary>
    public DialogueNode CreateDialogueNode(string nodeName, Vector2 position)
    {
        var dialogueNode = new DialogueNode
        {
            title = nodeName,
            DialogueText = nodeName,
            GUID = Guid.NewGuid().ToString(),
        };

        // Добавляем входной порт
        var inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        dialogueNode.inputContainer.Add(inputPort);

        // Стиль узла
        dialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));

        // Кнопка добавления выбора
        var addChoiceButton = new Button(() => AddChoicePort(dialogueNode)) { text = "New Choice" };
        dialogueNode.titleContainer.Add(addChoiceButton);

        // Текстовое поле диалога
        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt =>
        {
            dialogueNode.DialogueText = evt.newValue;
            dialogueNode.title = evt.newValue;
        });
        textField.SetValueWithoutNotify(dialogueNode.title);
        dialogueNode.mainContainer.Add(textField);

        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
        dialogueNode.SetPosition(new Rect(position, defaultNodeSize));

        return dialogueNode;
    }

    /// <summary>
    /// Добавление порта выбора
    /// </summary>
    public void AddChoicePort(DialogueNode dialogueNode, string overriddenPortName = "")
    {
        var generatedPort = GeneratePort(dialogueNode, Direction.Output);
        var outputPortCount = dialogueNode.outputContainer.Children().Count();

        // Настраиваем имя порта
        var choicePortName = string.IsNullOrEmpty(overriddenPortName)
            ? $"Choice {outputPortCount + 1}"
            : overriddenPortName;

        generatedPort.portName = choicePortName;

        // Удаляем стандартный лейбл
        generatedPort.contentContainer.Remove(generatedPort.contentContainer.Q<Label>("type"));

        // Поле для редактирования имени порта
        var textField = new TextField { value = choicePortName };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label(" "));
        generatedPort.contentContainer.Add(textField);

        // Кнопка удаления порта
        var deleteButton = new Button(() => RemovePort(dialogueNode, generatedPort))
        { text = "X" };
        generatedPort.contentContainer.Add(deleteButton);

        dialogueNode.outputContainer.Add(generatedPort);
        dialogueNode.RefreshPorts();
        dialogueNode.RefreshExpandedState();
    }

    /// <summary>
    /// Удаление порта выбора
    /// </summary>
    private void RemovePort(DialogueNode dialogueNode, Port generatedPort)
    {
        // Находим связанные связи
        var targetEdge = edges.ToList()
            .Where(x => x.output == generatedPort && x.output.node == generatedPort.node);

        // Удаляем связи
        if (targetEdge.Any())
        {
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(edge);
        }

        dialogueNode.outputContainer.Remove(generatedPort);
        dialogueNode.RefreshPorts();
        dialogueNode.RefreshExpandedState();
    }
    #endregion

    #region Blackboard Operations
    /// <summary>
    /// Очистка свойств черной доски
    /// </summary>
    public void ClearBlackBoardAndExposedProperties()
    {
        ExposedProperties.Clear();
        Blackboard.Clear();
    }

    /// <summary>
    /// Добавление свойства на черную доску
    /// </summary>
    public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
    {
        var localProperty = new ExposedProperty
        {
            PropertyName = exposedProperty.PropertyName,
            PropertyValue = exposedProperty.PropertyValue
        };

        ExposedProperties.Add(localProperty);

        // Создаем элемент черной доски
        var container = new VisualElement();
        var field = new BlackboardField { text = localProperty.PropertyName, typeText = "string" };

        // Поле для значения свойства
        var valueField = new TextField("Value:") { value = localProperty.PropertyValue };
        valueField.RegisterValueChangedCallback(evt =>
        {
            var index = ExposedProperties.FindIndex(x => x.PropertyName == localProperty.PropertyName);
            ExposedProperties[index].PropertyValue = evt.newValue;
        });

        var row = new BlackboardRow(field, valueField);
        container.Add(row);
        Blackboard.Add(container);
    }
    #endregion
}