using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

/// <summary>
/// Граф для редактирования диалогов с ограничениями на соединения узлов
/// Обеспечивает правильное подключение SpeechNode и OptionNode
/// </summary>
public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(250, 300);
    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();

    private EditorWindow editorWindow;
    private NodeSearchWindow searchWindow;

    public DialogueGraphView(EditorWindow editorWindow)
    {
        this.editorWindow = editorWindow;

        // Загружаем стили для графа
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));

        // Настраиваем масштабирование
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // Добавляем манипуляторы для перемещения и выделения
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Добавляем сетку в качестве фона
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Создаем стартовый узел
        AddElement(NodeFactory.CreateEntryNode(new Vector2(100, 200)));

        // Добавляем окно поиска узлов
        AddSearchWindow();

        // Создаем черную доску для свойств
        GenerateBlackBoard();

        // Регистрируем обработчик нажатия клавиш для удаления узлов
        this.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    /// <summary>
    /// Добавляет окно поиска узлов в граф
    /// </summary>
    private void AddSearchWindow()
    {
        searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        searchWindow.Init(editorWindow, this);

        // Настраиваем создание узлов через окно поиска
        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    /// <summary>
    /// Обработчик нажатия клавиш для удаления узлов
    /// </summary>
    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Delete)
        {
            // Проверяем, есть ли в выделении EntryNode
            if (selection.OfType<BaseNode>().Any(node => node.EntryPoint))
            {
                EditorUtility.DisplayDialog("Cannot Delete", "The entry point node cannot be deleted.", "OK");
                evt.StopPropagation();
                return;
            }

            // Удаляем выбранные элементы
            DeleteSelection();
            evt.StopPropagation();
        }
    }

    /// <summary>
    /// Определяет совместимые порты для соединения с учетом ограничений
    /// SpeechNode можно соединять только с OptionNode, OptionNode можно соединять только с SpeechNode
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        // Перебираем все порты в графе
        ports.ForEach(port =>
        {
            // Пропускаем тот же порт, порт того же узла и порты с тем же направлением
            if (startPort != port &&
                startPort.node != port.node &&
                startPort.direction != port.direction)
            {
                // Проверяем ограничения на соединения узлов
                if (IsConnectionAllowed(startPort, port))
                {
                    compatiblePorts.Add(port);
                }
            }
        });

        return compatiblePorts;
    }

    /// <summary>
    /// Проверяет, разрешено ли соединение между двумя портами
    /// SpeechNode можно соединять только с OptionNode, OptionNode можно соединять только с SpeechNode
    /// </summary>
    private bool IsConnectionAllowed(Port startPort, Port targetPort)
    {
        var startNode = startPort.node as BaseNode;
        var targetNode = targetPort.node as BaseNode;

        // Определяем направление соединения
        if (startPort.direction == Direction.Output)
        {
            // Соединение от startNode к targetNode

            // SpeechNode можно соединять только с OptionNode
            if (startNode is SpeechNode)
            {
                return targetNode is OptionNode;
            }

            // OptionNode можно соединять только с SpeechNode
            if (startNode is OptionNode)
            {
                return targetNode is SpeechNode;
            }

            // EntryNode можно соединять только с SpeechNode
            if (startNode is EntryNode)
            {
                return targetNode is SpeechNode;
            }
        }
        else if (startPort.direction == Direction.Input)
        {
            // Соединение от targetNode к startNode

            // SpeechNode можно соединять только с OptionNode или EntryNode
            if (startNode is SpeechNode)
            {
                return targetNode is OptionNode || targetNode is EntryNode;
            }

            // OptionNode можно соединять только с SpeechNode
            if (startNode is OptionNode)
            {
                return targetNode is SpeechNode;
            }
        }

        return false;
    }

    /// <summary>
    /// Создает узел указанного типа в заданной позиции
    /// </summary>
    public void CreateNode(System.Type nodeType, Vector2 position)
    {
        var node = NodeFactory.CreateNode(nodeType, position);
        if (node != null)
        {
            AddElement(node);
        }
    }

    /// <summary>
    /// Создает черную доску для exposed properties
    /// </summary>
    private void GenerateBlackBoard()
    {
        Blackboard = new Blackboard(this);
        Blackboard.title = "Exposed Properties";
        Blackboard.Add(new BlackboardSection { title = "Exposed Properties" });

        // Обработчик добавления нового свойства
        Blackboard.addItemRequested = blackboard =>
        {
            AddPropertyToBlackBoard(new ExposedProperty());
        };

        // Обработчик редактирования имени свойства
        Blackboard.editTextRequested = (blackboard, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField)element).text;
            if (ExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exists, please chose another one.", "OK");
                return;
            }

            var propertyIndex = ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            ExposedProperties[propertyIndex].PropertyName = newValue;
            ((BlackboardField)element).text = newValue;
        };

        // Добавляем черную доску в граф
        Add(Blackboard);
    }

    /// <summary>
    /// Добавляет свойство на черную доску
    /// </summary>
    public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
    {
        var localProperty = new ExposedProperty
        {
            PropertyName = exposedProperty.PropertyName,
            PropertyValue = exposedProperty.PropertyValue
        };

        ExposedProperties.Add(localProperty);

        // Создаем контейнер для свойства
        var container = new VisualElement();
        var blackboardField = new BlackboardField
        {
            text = localProperty.PropertyName,
            typeText = "String"
        };

        // Поле для значения свойства
        var propertyValueTextField = new TextField("Value:")
        {
            value = localProperty.PropertyValue
        };

        // Обработчик изменения значения свойства
        propertyValueTextField.RegisterValueChangedCallback(evt =>
        {
            var changingPropertyIndex = ExposedProperties.FindIndex(x => x.PropertyName == localProperty.PropertyName);
            ExposedProperties[changingPropertyIndex].PropertyValue = evt.newValue;
        });

        // Создаем строку для отображения свойства
        var blackboardValueRow = new BlackboardRow(blackboardField, propertyValueTextField);
        container.Add(blackboardValueRow);

        // Добавляем свойство на черную доску
        Blackboard.Add(container);
    }

    /// <summary>
    /// Удаляет выбранные элементы из графа
    /// </summary>
    private void DeleteSelection()
    {
        // Создаем копию выделения для безопасного удаления
        var selectionCopy = selection.ToList();

        foreach (var selectedElement in selectionCopy)
        {
            if (selectedElement is BaseNode node)
            {
                // Удаляем связанные связи
                var edgesToRemove = edges.ToList().Where(e => e.input.node == node || e.output.node == node).ToList();
                foreach (var edge in edgesToRemove)
                {
                    RemoveElement(edge);
                }

                // Удаляем узел
                RemoveElement(node);
            }
            else if (selectedElement is Edge edge)
            {
                // Удаляем связь
                RemoveElement(edge);
            }
        }
    }

    // Добавим этот метод в класс DialogueGraphView
    /// <summary>
    /// Очистка свойств черной доски
    /// </summary>
    public void ClearBlackBoardAndExposedProperties()
    {
        ExposedProperties.Clear();
        Blackboard.Clear();
    }
}