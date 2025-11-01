using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;
using UnityEditor.UIElements;

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

    public bool _hasUnsavedChangesWithoutFile = false;
    public bool _unsavedChangesWarningShown = false;

    private VisualElement _highlightedNode;
    private StyleColor _originalNodeBgColor;

    private string _baseCharacterGuid;

    private VisualElement _customBackground;
    private GridBackground _gridBackground;

    private TextEditorModalWindow _activeTextEditorWindow;
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
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // === ЗАМЕНА: инициализируем оба фона, но показываем только один ===
        _gridBackground = new GridBackground();
        _customBackground = new VisualElement();
        _customBackground.StretchToParentSize();

        // Пока не знаем настройки — показываем сетку по умолчанию
        Insert(0, _gridBackground);
        _gridBackground.StretchToParentSize();

        AddElement(NodeFactory.CreateEntryNode(new Vector2(100, 200)));
        AddSearchWindow();
        GenerateBlackBoard();
        this.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    public void UpdateGraphBackgroundInternal()
    {
        // Загружаем актуальные настройки
        string[] guids = AssetDatabase.FindAssets("t:DialogueSettingsData");
        DialogueSettingsData settings = null;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<DialogueSettingsData>(path);
        }

        if (settings == null)
            return;

        bool useCustom = settings.UI.UseCustomBackgroundColor;
        Color customColor = settings.UI.CustomBackgroundColor;

        // Убираем текущий фон
        if (_gridBackground.parent != null)
            _gridBackground.RemoveFromHierarchy();
        if (_customBackground.parent != null)
            _customBackground.RemoveFromHierarchy();

        // Вставляем нужный
        if (useCustom)
        {
            _customBackground.style.backgroundColor = new StyleColor(customColor);
            Insert(0, _customBackground);
        }
        else
        {
            Insert(0, _gridBackground);
            _gridBackground.StretchToParentSize();
        }
    }

    public static void UpdateGraphBackgroundForAllInstances()
    {
        // Находим все открытые окна DialogueGraph
        var graphWindows = Resources.FindObjectsOfTypeAll<DialogueGraph>();
        foreach (var window in graphWindows)
        {
            if (window.graphView != null)
                window.graphView.UpdateGraphBackgroundInternal();
        }
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
    /// Проверяет, разрешено ли соединение между портами.
    /// Запрещает любые соединения между OptionNode и его подтипами.
    /// Все остальные соединения разрешены без ограничений.
    /// </summary>
    private bool IsConnectionAllowed(Port startPort, Port targetPort)
    {
        var startNode = startPort.node as BaseNode;
        var targetNode = targetPort.node as BaseNode;

        if (startNode == null || targetNode == null)
            return false;

        // Проверяем, являются ли оба узла OptionNode (включая подтипы)
        bool IsOptionNode(BaseNode node) =>
            node is OptionNode || node is OptionNodeText || node is OptionNodeAudio || node is OptionNodeImage;

        if (IsOptionNode(startNode) && IsOptionNode(targetNode))
            return false;

        // Все остальные соединения разрешены
        return true;
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
            MarkUnsavedChangeWithoutFile();
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
    /// Добавляет Int-свойство в Blackboard и в список IntExposedProperties
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
        // Поля для редактирования параметров свойства
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
            MarkUnsavedChangeWithoutFile(); // ← добавлено
        });
        minField.RegisterValueChangedCallback(evt =>
        {
            intProperty.MinValue = evt.newValue;
            if (intProperty.IntValue < evt.newValue)
                valueField.value = evt.newValue;
            MarkUnsavedChangeWithoutFile(); // ← добавлено
        });
        maxField.RegisterValueChangedCallback(evt =>
        {
            intProperty.MaxValue = evt.newValue;
            if (intProperty.IntValue > evt.newValue)
                valueField.value = evt.newValue;
            MarkUnsavedChangeWithoutFile(); // ← добавлено
        });
        valueField.RegisterValueChangedCallback(evt =>
        {
            intProperty.IntValue = Mathf.Clamp(evt.newValue, intProperty.MinValue, intProperty.MaxValue);
            valueField.value = intProperty.IntValue;
            MarkUnsavedChangeWithoutFile(); // ← добавлено
        });

        container.Add(blackboardField);
        container.Add(nameField);
        container.Add(minField);
        container.Add(maxField);
        container.Add(valueField);

        // Добавляем в секцию Int
        IntSection.Add(container);

        // Контекстное меню для удаления
        blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", action =>
            {
                int usageCount = 0;
                var conditionNodes = nodes.ToList().OfType<IntConditionNode>();
                var modifyNodes = nodes.ToList().OfType<ModifyIntNode>();
                foreach (var node in conditionNodes)
                    if (node.SelectedProperty == intProperty.PropertyName) usageCount++;
                foreach (var node in modifyNodes)
                    if (node.SelectedProperty == intProperty.PropertyName) usageCount++;

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
    /// Добавляет String-свойство в Blackboard и в список StringExposedProperties
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
            MarkUnsavedChangeWithoutFile(); // ← добавлено
        });
        valueField.RegisterValueChangedCallback(evt =>
        {
            stringProperty.StringValue = evt.newValue;
            MarkUnsavedChangeWithoutFile(); // ← добавлено
        });

        container.Add(blackboardField);
        container.Add(nameField);
        container.Add(valueField);

        StringSection.Add(container);

        blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Delete", action =>
            {
                int usageCount = 0;
                var conditionNodes = nodes.ToList().OfType<StringConditionNode>();
                foreach (var node in conditionNodes)
                    if (node.SelectedProperty == stringProperty.PropertyName) usageCount++;

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
    /// Удаляет Int-свойство из Blackboard и из списка
    /// </summary>
    private void RemoveIntProperty(IntExposedProperty property, VisualElement container)
    {
        IntExposedProperties.Remove(property);

        // Очищаем использование в узлах
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

        RefreshAllPropertyNodes();
        IntSection.Remove(container);
        UpdateSectionTitles();
        MarkUnsavedChangeWithoutFile(); // ← добавлено
    }

    /// <summary>
    /// Удаляет String-свойство из Blackboard и из списка
    /// </summary>
    private void RemoveStringProperty(StringExposedProperty property, VisualElement container)
    {
        StringExposedProperties.Remove(property);

        // Очищаем использование в узлах
        var conditionNodes = nodes.ToList().OfType<StringConditionNode>();
        foreach (var node in conditionNodes)
        {
            if (node.SelectedProperty == property.PropertyName)
            {
                node.SelectedProperty = "";
                Debug.LogWarning($"Variable {property.PropertyName} was removed but was used in StringConditionNode {node.GUID}");
            }
        }

        RefreshAllPropertyNodes();
        StringSection.Remove(container);
        UpdateSectionTitles();
        MarkUnsavedChangeWithoutFile(); // ← добавлено
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
    /// Полностью очищает все exposed properties из Blackboard
    /// </summary>
    private void ClearAllProperties()
    {
        if (!EditorUtility.DisplayDialog("Clear All Properties",
            "Are you sure you want to remove all exposed properties?", "Yes", "No"))
        {
            return;
        }

        IntExposedProperties.Clear();
        StringExposedProperties.Clear();

        IntSection?.Clear();
        StringSection?.Clear();

        RefreshAllPropertyNodes();
        UpdateSectionTitles();
        Debug.Log("All exposed properties cleared successfully");
        MarkUnsavedChangeWithoutFile(); // ← добавлено
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
        var selectionCopy = selection.ToList();
        bool hadChange = false;
        foreach (var selectedElement in selectionCopy)
        {
            if (selectedElement is BaseNode node)
            {
                // Используем this.edges, а не Edges
                var edgesToRemove = this.edges
                    .Where(e => e.input.node == node || e.output.node == node)
                    .ToList(); // ← ToList() делает его IList
                foreach (var edge in edgesToRemove)
                {
                    RemoveElement(edge);
                }
                RemoveElement(node);
                hadChange = true;
            }
            else if (selectedElement is Edge edge)
            {
                RemoveElement(edge);
                hadChange = true;
            }
        }
        if (hadChange)
            MarkUnsavedChangeWithoutFile();
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
    private void ClearGraph()
    {
        // Удаляем все узлы, кроме EntryPoint
        var nodesToRemove = this.nodes.ToList().Where(node => !(node is EntryNode)).ToList();
        foreach (var node in nodesToRemove)
        {
            // Удаляем связанные рёбра
            var edgesToRemove = this.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();
            foreach (var edge in edgesToRemove)
                RemoveElement(edge);

            RemoveElement(node);
        }
        // Очищаем Blackboard и exposed properties
        ClearBlackBoardAndExposedProperties();
        // Сбрасываем BaseCharacterGuid
        BaseCharacterGuid = string.Empty;
    }

    /// <summary>
    /// Отмечает, что были внесены изменения без привязки к файлу.
    /// Вызывается при добавлении/удалении узлов, связей, изменении Blackboard или Base Character.
    /// </summary>
    public void MarkUnsavedChangeWithoutFile()
    {
        var assetField = this.parent?.parent?.Q<ObjectField>("Dialogue File");
        if (assetField?.value == null)
        {
            _hasUnsavedChangesWithoutFile = true;
            if (!_unsavedChangesWarningShown)
            {
                _unsavedChangesWarningShown = true;
                Debug.LogWarning("Диалог не был выбран");
            }
        }
        else
        {
            // Если файл уже выбран, сбрасываем флаг
            _hasUnsavedChangesWithoutFile = false;
            _unsavedChangesWarningShown = false;
        }
    }

    public void OpenTextEditor(string initialText, string nodeGuid, Action<string> onTextChanged)
    {
        // Закрываем предыдущее окно (оно само вызовет ClearNodeHighlight)
        _activeTextEditorWindow?.Close();
        _activeTextEditorWindow = null;

        // ←←← КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: явно сбросить выделение, даже если окно не было открыто
        ClearNodeHighlight();

        // Находим целевой узел
        var targetNode = nodes.ToList().FirstOrDefault(n => n is BaseNode node && node.GUID == nodeGuid) as VisualElement;
        if (targetNode != null)
        {
            _originalNodeBgColor = targetNode.style.backgroundColor;
            targetNode.style.backgroundColor = new StyleColor(new Color(0.3f, 0.6f, 1f, 0.2f));
            _highlightedNode = targetNode;
        }

        _activeTextEditorWindow = new TextEditorModalWindow(this, initialText, nodeGuid, onTextChanged);
        _activeTextEditorWindow.style.position = Position.Absolute;
        _activeTextEditorWindow.style.top = 30;
        _activeTextEditorWindow.style.right = 0;
        Add(_activeTextEditorWindow);

        ScrollToNode(nodeGuid);
    }

    public void ScrollToNode(string guid)
    {
        var targetNode = nodes.ToList().FirstOrDefault(n => n is BaseNode node && node.GUID == guid);
        if (targetNode == null) return;

        var nodeRect = targetNode.GetPosition();
        var center = nodeRect.center;

        // Размер области просмотра
        float viewWidth = this.layout.width;
        float viewHeight = this.layout.height;

        // Целевая позиция прокрутки с отступами 50px
        Vector2 targetScroll = center - new Vector2(viewWidth * 0.5f - 50f, viewHeight * 0.5f - 50f);

        // Применяем с небольшой задержкой для корректного layout
        contentViewContainer.schedule.Execute(() =>
        {
            SetViewScroll(targetScroll);
        }).ExecuteLater(50);
    }

    private void SetViewScroll(Vector2 scrollPosition)
    {
        var viewType = typeof(GraphView);
        var field = viewType.GetField("_viewTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var matrix = Matrix4x4.Translate(new Vector3(-scrollPosition.x, -scrollPosition.y, 0));
            field.SetValue(this, matrix);
        }
    }

    public void ClearNodeSelection()
    {
        foreach (var element in selection.ToList())
        {
            if (element is VisualElement visualElement)
            {
                visualElement.RemoveFromClassList("selected");
            }
        }
        selection.Clear();
    }

    public void ClearNodeHighlight()
    {
        if (_highlightedNode != null && _highlightedNode.parent != null)
            _highlightedNode.style.backgroundColor = _originalNodeBgColor;

        _highlightedNode = null;
    }
}