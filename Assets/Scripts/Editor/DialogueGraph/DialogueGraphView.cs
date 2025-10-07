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

    public List<IntExposedProperty> IntExposedProperties = new List<IntExposedProperty>();
    public List<StringExposedProperty> StringExposedProperties = new List<StringExposedProperty>();

    private VisualElement[] BlackboardSections;

    private EditorWindow editorWindow;
    private NodeSearchWindow searchWindow;

    private string _baseCharacterGuid;
    public string BaseCharacterGuid
    {
        get => _baseCharacterGuid;
        set
        {
            if (_baseCharacterGuid != value)
            {
                _baseCharacterGuid = value;
                OnBaseCharacterChanged();
            }
        }
    }

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
    /// Проверяет, разрешено ли соединение между портами
    /// SpeechNode может соединяться только с OptionNode, OptionNode может соединяться только с SpeechNode
    /// EndNode может соединяться только с OptionNode
    /// </summary>
    private bool IsConnectionAllowed(Port startPort, Port targetPort)
    {
        var startNode = startPort.node as BaseNode;
        var targetNode = targetPort.node as BaseNode;

        if (startPort.direction == Direction.Output)
        {
            return (startNode, targetNode) switch
            {
                (SpeechNode, SpeechNode) => true,
                (SpeechNode, OptionNode) => true,
                (SpeechNode, IntConditionNode) => true,
                (SpeechNode, StringConditionNode) => true,
                (OptionNode, SpeechNode) => true,
                (OptionNode, IntConditionNode) => true,
                (OptionNode, StringConditionNode) => true,
                (OptionNode, EndNode) => true, // Разрешаем подключение от OptionNode к EndNode
                (EntryNode, SpeechNode) => true,
                (ModifyIntNode, SpeechNode) => true,
                (ModifyIntNode, OptionNode) => true,
                (ModifyIntNode, IntConditionNode) => true,
                (ModifyIntNode, StringConditionNode) => true,
                (IntConditionNode, OptionNode) => IsConditionNodeConnectedToSpeech(startNode as IntConditionNode),
                (IntConditionNode, SpeechNode) => IsConditionNodeConnectedToOption(startNode as IntConditionNode),
                (StringConditionNode, OptionNode) => IsConditionNodeConnectedToSpeech(startNode as StringConditionNode),
                (StringConditionNode, SpeechNode) => IsConditionNodeConnectedToOption(startNode as StringConditionNode),
                (_, EndNode) => false, // Запрещаем подключение к EndNode от любых других узлов
                _ => false
            };
        }
        else
        {
            return (startNode, targetNode) switch
            {
                (SpeechNode, SpeechNode) => true,
                (SpeechNode, OptionNode) => true,
                (SpeechNode, IntConditionNode) => true,
                (SpeechNode, StringConditionNode) => true,
                (OptionNode, SpeechNode) => true,
                (OptionNode, IntConditionNode) => true,
                (OptionNode, StringConditionNode) => true,
                (EndNode, OptionNode) => true, // Разрешаем подключение от EndNode к OptionNode (входной порт)
                (ModifyIntNode, SpeechNode) => true,
                (ModifyIntNode, OptionNode) => true,
                (ModifyIntNode, IntConditionNode) => true,
                (ModifyIntNode, StringConditionNode) => true,
                (IntConditionNode, OptionNode) => true,
                (IntConditionNode, SpeechNode) => true,
                (StringConditionNode, OptionNode) => true,
                (StringConditionNode, SpeechNode) => true,
                _ => false
            };
        }
    } 

    /// <summary>
    /// Проверяет, подключен ли узел условия к SpeechNode
    /// </summary>
    private bool IsConditionNodeConnectedToSpeech(BaseConditionNode conditionNode)
    {
        if (conditionNode == null) return false;

        var inputPort = conditionNode.inputContainer.Children().FirstOrDefault() as Port;
        return inputPort != null && inputPort.connections.Any(edge =>
            edge.output.node is SpeechNode);
    }

    /// <summary>
    /// Проверяет, подключен ли узел условия к OptionNode
    /// </summary>
    private bool IsConditionNodeConnectedToOption(BaseConditionNode conditionNode)
    {
        if (conditionNode == null) return false;

        var inputPort = conditionNode.inputContainer.Children().FirstOrDefault() as Port;
        return inputPort != null && inputPort.connections.Any(edge =>
            edge.output.node is OptionNode);
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
    /// Создать черную доску для exposed properties с поддержкой скроллинга
    /// </summary>
    private void GenerateBlackBoard()
    {
        Blackboard = new Blackboard(this);
        Blackboard.title = "Exposed Properties";

        // Устанавливаем фиксированные размеры для Blackboard
        Blackboard.style.minWidth = 300;
        Blackboard.style.maxWidth = 400;
        Blackboard.style.minHeight = 200;
        Blackboard.style.maxHeight = 500;

        // Создаем основной скроллвью
        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // Создаем секции для разных типов свойств
        var intSection = new BlackboardSection { title = "Int Properties (0)" };
        var stringSection = new BlackboardSection { title = "String Properties (0)" };

        // Добавляем секции в скроллвью
        scrollView.Add(intSection);
        scrollView.Add(stringSection);

        // Добавляем скроллвью в Blackboard
        Blackboard.Add(scrollView);

        // Сохраняем ссылки на секции для последующего использования
        IntSection = intSection;
        StringSection = stringSection;

        // Функционал добавления новых свойств
        Blackboard.addItemRequested = blackboard =>
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Int Property"), false, () =>
            {
                var newProperty = new IntExposedProperty();
                IntExposedProperties.Add(newProperty);
                AddIntPropertyToBlackBoard(newProperty);
                UpdateSectionTitles();
            });
            menu.AddItem(new GUIContent("Add String Property"), false, () =>
            {
                var newProperty = new StringExposedProperty();
                StringExposedProperties.Add(newProperty);
                AddStringPropertyToBlackBoard(newProperty);
                UpdateSectionTitles();
            });
            menu.ShowAsContext();
        };

        // Функционал редактирования имен свойств
        Blackboard.editTextRequested = (blackboard, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField)element).text;

            // Проверяем уникальность имени свойства
            if (IntExposedProperties.Any(x => x.PropertyName == newValue) ||
                StringExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exists, please choose another one.", "OK");
                return;
            }

            // Обновляем имя в Int свойствах
            var intPropertyIndex = IntExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            if (intPropertyIndex >= 0)
            {
                IntExposedProperties[intPropertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;

                // Обновляем все узлы, которые используют это свойство
                RefreshAllPropertyNodes();
                return;
            }

            // Обновляем имя в String свойствах
            var stringPropertyIndex = StringExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            if (stringPropertyIndex >= 0)
            {
                StringExposedProperties[stringPropertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;

                // Обновляем все узлы, которые используют это свойство
                RefreshAllPropertyNodes();
            }
        };

        // Добавляем кнопку очистки всех свойств
        var clearButton = new Button(ClearAllProperties)
        {
            text = "Clear All Properties"
        };

        clearButton.style.marginTop = 5;
        clearButton.style.marginLeft = 5;
        clearButton.style.marginRight = 5;
        clearButton.style.marginBottom = 5;
        Blackboard.Add(clearButton);

        // Добавляем Blackboard в граф
        Add(Blackboard);

        // Обновляем отображение количества свойств
        UpdateSectionTitles();
    }

    // Добавьте эти поля в класс DialogueGraphView:
    private BlackboardSection IntSection;
    private BlackboardSection StringSection;

    /// <summary>
    /// Добавить Int свойство на черную доску
    /// </summary>
    private void AddIntPropertyToBlackBoard(IntExposedProperty intProperty)
    {
        var container = new VisualElement();
        container.style.marginBottom = 5;

        var blackboardField = new BlackboardField
        {
            text = intProperty.PropertyName,
            typeText = "Int"
        };

        // Поля для редактирования свойства
        var nameField = new TextField("Name:") { value = intProperty.PropertyName };
        var minField = new IntegerField("Min:") { value = intProperty.MinValue };
        var maxField = new IntegerField("Max:") { value = intProperty.MaxValue };
        var valueField = new IntegerField("Value:") { value = intProperty.IntValue };

        // Обработчики изменений
        nameField.RegisterValueChangedCallback(evt =>
        {
            intProperty.PropertyName = evt.newValue;
            blackboardField.text = evt.newValue;
            UpdateSectionTitles();
            RefreshAllPropertyNodes();
        });

        minField.RegisterValueChangedCallback(evt =>
        {
            intProperty.MinValue = evt.newValue;
            if (intProperty.IntValue < evt.newValue)
                valueField.value = evt.newValue;
        });

        maxField.RegisterValueChangedCallback(evt =>
        {
            intProperty.MaxValue = evt.newValue;
            if (intProperty.IntValue > evt.newValue)
                valueField.value = evt.newValue;
        });

        valueField.RegisterValueChangedCallback(evt =>
        {
            intProperty.IntValue = Mathf.Clamp(evt.newValue, intProperty.MinValue, intProperty.MaxValue);
            valueField.value = intProperty.IntValue;
        });

        container.Add(blackboardField);
        container.Add(nameField);
        container.Add(minField);
        container.Add(maxField);
        container.Add(valueField);

        // Добавляем в секцию Int свойств
        IntSection.Add(container);

        // Контекстное меню для удаления
        blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", action =>
            {
                // Проверяем использование свойства
                int usageCount = 0;
                var conditionNodes = nodes.ToList().OfType<IntConditionNode>();
                var modifyNodes = nodes.ToList().OfType<ModifyIntNode>();

                foreach (var node in conditionNodes)
                {
                    if (node.SelectedProperty == intProperty.PropertyName)
                        usageCount++;
                }

                foreach (var node in modifyNodes)
                {
                    if (node.SelectedProperty == intProperty.PropertyName)
                        usageCount++;
                }

                // Подтверждение удаления
                if (EditorUtility.DisplayDialog("Confirm Delete",
                    $"Property '{intProperty.PropertyName}' is used in {usageCount} nodes.\nDelete anyway?",
                    "Delete", "Cancel"))
                {
                    RemoveIntProperty(intProperty, container);
                }
            });
        }));
    }

    /// <summary>
    /// Добавить String свойство на черную доску
    /// </summary>
    private void AddStringPropertyToBlackBoard(StringExposedProperty stringProperty)
    {
        var container = new VisualElement();
        container.style.marginBottom = 5;

        var blackboardField = new BlackboardField
        {
            text = stringProperty.PropertyName,
            typeText = "String"
        };

        var nameField = new TextField("Name:") { value = stringProperty.PropertyName };
        var valueField = new TextField("Value:") { value = stringProperty.StringValue };

        nameField.RegisterValueChangedCallback(evt =>
        {
            stringProperty.PropertyName = evt.newValue;
            blackboardField.text = evt.newValue;
            UpdateSectionTitles();
            RefreshAllPropertyNodes();
        });

        valueField.RegisterValueChangedCallback(evt =>
        {
            stringProperty.StringValue = evt.newValue;
        });

        container.Add(blackboardField);
        container.Add(nameField);
        container.Add(valueField);

        // Добавляем в секцию String свойств
        StringSection.Add(container);

        // Контекстное меню для удаления
        blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", action =>
            {
                // Проверяем использование свойства
                int usageCount = 0;
                var conditionNodes = nodes.ToList().OfType<StringConditionNode>();
                foreach (var node in conditionNodes)
                {
                    if (node.SelectedProperty == stringProperty.PropertyName)
                        usageCount++;
                }

                // Подтверждение удаления
                if (EditorUtility.DisplayDialog("Confirm Delete",
                    $"Property '{stringProperty.PropertyName}' is used in {usageCount} condition nodes.\nDelete anyway?",
                    "Delete", "Cancel"))
                {
                    RemoveStringProperty(stringProperty, container);
                }
            });
        }));
    }

    /// <summary>
    /// Удалить Int свойство
    /// </summary>
    private void RemoveIntProperty(IntExposedProperty property, VisualElement container)
    {
        // Удаляем из списка
        IntExposedProperties.Remove(property);

        // Очищаем узлы, которые использовали это свойство
        var conditionNodes = nodes.ToList().OfType<IntConditionNode>();
        var modifyNodes = nodes.ToList().OfType<ModifyIntNode>();

        foreach (var node in conditionNodes)
        {
            if (node.SelectedProperty == property.PropertyName)
            {
                node.SelectedProperty = "";
                Debug.LogWarning($"Variable {property.PropertyName} was removed but was used in IntConditionNode {node.GUID}");
            }
        }

        foreach (var node in modifyNodes)
        {
            if (node.SelectedProperty == property.PropertyName)
            {
                node.SelectedProperty = "";
                Debug.LogWarning($"Variable {property.PropertyName} was removed but was used in ModifyIntNode {node.GUID}");
            }
        }

        // Обновляем все узлы, которые используют свойства
        RefreshAllPropertyNodes();

        // Удаляем визуальный элемент
        IntSection.Remove(container);

        // Обновляем заголовок секции
        UpdateSectionTitles();
    }

    /// <summary>
    /// Удалить String свойство
    /// </summary>
    private void RemoveStringProperty(StringExposedProperty property, VisualElement container)
    {
        // Удаляем из списка
        StringExposedProperties.Remove(property);

        // Очищаем узлы, которые использовали это свойство
        var conditionNodes = nodes.ToList().OfType<StringConditionNode>();
        foreach (var node in conditionNodes)
        {
            if (node.SelectedProperty == property.PropertyName)
            {
                node.SelectedProperty = "";
                Debug.LogWarning($"Variable {property.PropertyName} was removed but was used in StringConditionNode {node.GUID}");
            }
        }

        // Обновляем все узлы, которые используют свойства
        RefreshAllPropertyNodes();

        // Удаляем визуальный элемент
        StringSection.Remove(container);

        // Обновляем заголовок секции
        UpdateSectionTitles();
    }

    /// <summary>
    /// Вспомогательный метод для обновления заголовков секций с количеством свойств
    /// </summary>
    private void UpdateSectionTitles()
    {
        if (IntSection != null)
        {
            IntSection.title = $"Int Properties ({IntExposedProperties.Count})";
        }

        if (StringSection != null)
        {
            StringSection.title = $"String Properties ({StringExposedProperties.Count})";
        }
    }

    /// <summary>
    /// Вспомогательный метод для обновления всех узлов, использующих свойства
    /// </summary>
    private void RefreshAllPropertyNodes()
    {
        var allPropertyNodes = nodes.ToList().OfType<IPropertyNode>();
        foreach (var node in allPropertyNodes)
        {
            node.RefreshPropertyDropdown();
        }
    }

    /// <summary>
    /// Вспомогательный метод для очистки всех свойств
    /// </summary>
    private void ClearAllProperties()
    {
        if (!EditorUtility.DisplayDialog("Clear All Properties",
            "Are you sure you want to remove all exposed properties?", "Yes", "No"))
        {
            return;
        }

        // Очищаем списки свойств
        IntExposedProperties.Clear();
        StringExposedProperties.Clear();

        // Очищаем визуальное представление
        if (IntSection != null)
            IntSection.Clear();

        if (StringSection != null)
            StringSection.Clear();

        // Обновляем все узлы, которые использовали свойства
        RefreshAllPropertyNodes();

        // Обновляем заголовки секций
        UpdateSectionTitles();

        Debug.Log("All exposed properties cleared successfully");
    }

    /// <summary>
    /// Очистить черную доску и exposed properties (для совместимости со старым кодом)
    /// </summary>
    public void ClearBlackBoardAndExposedProperties()
    {
        ClearAllProperties();
    }

    /// <summary>
    /// Добавляет свойство на черную доску
    /// </summary>
    // <summary>
    /// Добавляет свойство на черную доску
    /// </summary>
    public void AddPropertyToBlackBoard(object property)
    {
        if (property is IntExposedProperty intProperty)
        {
            IntExposedProperties.Add(intProperty);

            var container = new VisualElement();
            var blackboardField = new BlackboardField
            {
                text = intProperty.PropertyName,
                typeText = "Int"
            };

            // Поля для редактирования int свойства
            var nameField = new TextField("Name:") { value = intProperty.PropertyName };
            var minField = new IntegerField("Min:") { value = intProperty.MinValue };
            var maxField = new IntegerField("Max:") { value = intProperty.MaxValue };
            var valueField = new IntegerField("Value:") { value = intProperty.IntValue };

            // Обработчики изменений
            nameField.RegisterValueChangedCallback(evt =>
            {
                intProperty.PropertyName = evt.newValue;
                blackboardField.text = evt.newValue;
            });

            minField.RegisterValueChangedCallback(evt =>
            {
                intProperty.MinValue = evt.newValue;
                if (intProperty.IntValue < evt.newValue)
                    valueField.value = evt.newValue;
            });

            maxField.RegisterValueChangedCallback(evt =>
            {
                intProperty.MaxValue = evt.newValue;
                if (intProperty.IntValue > evt.newValue)
                    valueField.value = evt.newValue;
            });

            valueField.RegisterValueChangedCallback(evt =>
            {
                intProperty.IntValue = Mathf.Clamp(evt.newValue, intProperty.MinValue, intProperty.MaxValue);
                valueField.value = intProperty.IntValue;
            });

            container.Add(blackboardField);
            container.Add(nameField);
            container.Add(minField);
            container.Add(maxField);
            container.Add(valueField);

            Blackboard[0].Add(container); // Добавляем в секцию int свойств

            blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete", action =>
                {
                // Находим использование свойства
                int usageCount = 0;
                var conditionNodes = nodes.ToList().OfType<IntConditionNode>();
                var modifyNodes = nodes.ToList().OfType<ModifyIntNode>();

                foreach (var node in conditionNodes)
                {
                    if (node.SelectedProperty == intProperty.PropertyName)
                        usageCount++;
                }

                foreach (var node in modifyNodes)
                {
                    if (node.SelectedProperty == intProperty.PropertyName)
                        usageCount++;
                }

                    // Диалог подтверждения
                    if (EditorUtility.DisplayDialog("Confirm Delete",
                        $"Property '{intProperty.PropertyName}' is used in {usageCount} nodes.\nDelete anyway?",
                        "Delete", "Cancel"))
                    {
                        // Удаляем из списка
                        IntExposedProperties.Remove(intProperty);

                        // Обновляем все узлы, которые использовали это свойство
                        foreach (var node in conditionNodes)
                        {
                            if (node.SelectedProperty == intProperty.PropertyName)
                            {
                                node.SelectedProperty = "";
                                Debug.LogError($"Error: Variable {intProperty.PropertyName} was removed but is still used in IntConditionNode {node.GUID}");
                            }
                        }

                        foreach (var node in modifyNodes)
                        {
                            if (node.SelectedProperty == intProperty.PropertyName)
                            {
                                node.SelectedProperty = "";
                                Debug.LogError($"Error: Variable {intProperty.PropertyName} was removed but is still used in ModifyIntNode {node.GUID}");
                            }
                        }

                        // ОБНОВЛЯЕМ ВСЕ УЗЛЫ С ВЫПАДАЮЩИМИ СПИСКАМИ
                        var allPropertyNodes = nodes.ToList().OfType<IPropertyNode>();
                        foreach (var node in allPropertyNodes)
                        {
                            node.RefreshPropertyDropdown();
                        }

                        // Находим и удаляем визуальный элемент по имени свойства
                        var containers = Blackboard[0].Children().ToList();
                        foreach (var cont in containers)
                        {
                            var field = cont.Q<BlackboardField>();
                            if (field != null && field.text == intProperty.PropertyName)
                            {
                                Blackboard[0].Remove(cont);
                                break;
                            }
                        }
                    }
                });
            }));
        }
        else if (property is StringExposedProperty stringProperty)
        {
            StringExposedProperties.Add(stringProperty);

            var container = new VisualElement();
            var blackboardField = new BlackboardField
            {
                text = stringProperty.PropertyName,
                typeText = "String"
            };

            var nameField = new TextField("Name:") { value = stringProperty.PropertyName };
            var valueField = new TextField("Value:") { value = stringProperty.StringValue };

            nameField.RegisterValueChangedCallback(evt =>
            {
                stringProperty.PropertyName = evt.newValue;
                blackboardField.text = evt.newValue;
            });

            valueField.RegisterValueChangedCallback(evt =>
            {
                stringProperty.StringValue = evt.newValue;
            });

            container.Add(blackboardField);
            container.Add(nameField);
            container.Add(valueField);

            Blackboard[1].Add(container); // Добавляем в секцию string свойств

            // Добавляем контекстное меню для string свойств
            blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete", action =>
                {
                    // Находим использование свойства
                    int usageCount = 0;
                    var conditionNodes = nodes.ToList().OfType<StringConditionNode>();
                    foreach (var node in conditionNodes)
                    {
                        if (node.SelectedProperty == stringProperty.PropertyName)
                            usageCount++;
                    }

                    // Диалог подтверждения
                    if (EditorUtility.DisplayDialog("Confirm Delete",
                        $"Property '{stringProperty.PropertyName}' is used in {usageCount} condition nodes.\nDelete anyway?",
                        "Delete", "Cancel"))
                    {
                        // Удаляем из списка
                        StringExposedProperties.Remove(stringProperty);

                        // Обновляем все узлы, которые использовали это свойство
                        foreach (var node in nodes.ToList().OfType<StringConditionNode>())
                        {
                            if (node.SelectedProperty == stringProperty.PropertyName)
                            {
                                node.SelectedProperty = "";
                                Debug.LogError($"Error: Variable {stringProperty.PropertyName} was removed but is still used in StringConditionNode {node.GUID}");
                            }
                        }

                        // ОБНОВЛЯЕМ ВСЕ УЗЛЫ С ВЫПАДАЮЩИМИ СПИСКАМИ
                        var allPropertyNodes = nodes.ToList().OfType<IPropertyNode>();
                        foreach (var node in allPropertyNodes)
                        {
                            node.RefreshPropertyDropdown();
                        }

                        // Находим и удаляем визуальный элемент по имени свойства
                        var containers = Blackboard[1].Children().ToList();
                        foreach (var cont in containers)
                        {
                            var field = cont.Q<BlackboardField>();
                            if (field != null && field.text == stringProperty.PropertyName)
                            {
                                Blackboard[1].Remove(cont);
                                break;
                            }
                        }
                    }
                });
            }));
        }
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

    // Метод для обработки изменений базового персонажа
    private void OnBaseCharacterChanged()
    {
        // Можно добавить дополнительную логику при изменении базового персонажа
        Debug.Log($"Base character changed to: {BaseCharacterGuid}");
    }

    /// <summary>
    /// Публичный метод для полной очистки графа (кроме EntryNode)
    /// </summary>
    public void ClearGraph()
    {
        // Удаляем все узлы, кроме EntryPoint
        var nodesToRemove = nodes.ToList().Where(node => !(node is EntryNode)).ToList();
        foreach (var node in nodesToRemove)
        {
            // Удаляем связанные рёбра
            var edgesToRemove = edges.ToList().Where(e => e.input.node == node || e.output.node == node).ToList();
            foreach (var edge in edgesToRemove)
            {
                RemoveElement(edge);
            }
            RemoveElement(node);
        }

        // Очищаем Blackboard и exposed properties
        ClearBlackBoardAndExposedProperties();

        // Сбрасываем BaseCharacterGuid
        BaseCharacterGuid = string.Empty;
    }
}