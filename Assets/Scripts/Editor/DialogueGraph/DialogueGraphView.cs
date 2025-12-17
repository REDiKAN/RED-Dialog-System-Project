using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;
using DialogueSystem;

/// <summary>
/// Граф для редактирования диалогов с новой логикой соединений
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
    public TextEditorModalWindow _activeTextEditorWindow;
    private Port _draggedOutputPort;
    private Vector2 _dragReleasePosition;
    public DialogueContainer containerCache { get; set; }
    public UndoManager undoManager;
    private bool isUndoRedoOperation = false;
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
        undoManager = new UndoManager(this);

        // Инициализация фонов
        _gridBackground = new GridBackground();
        _customBackground = new VisualElement();
        _customBackground.StretchToParentSize();

        // Показываем сетку по умолчанию
        Insert(0, _gridBackground);
        _gridBackground.StretchToParentSize();

        AddElement(NodeFactory.CreateEntryNode(new Vector2(100, 200)));
        AddSearchWindow();
        this.RegisterCallback<PointerDownEvent>(OnPortPointerDown, TrickleDown.TrickleDown);
        GenerateBlackBoard();
        this.RegisterCallback<KeyDownEvent>(OnKeyDown);
        RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);

        // Добавляем обработчик изменения графа
        this.graphViewChanged += OnGraphViewChanged;

        EnsureGridIsVisible();
    }

    public void EnsureGridIsVisible()
    {
        // Проверяем, есть ли у нас настройки
        var settings = LoadDialogueSettings();
        if (settings == null) return;

        // Если используется пользовательский фон, но мы хотим видеть сетку
        if (settings.UI.UseCustomBackgroundColor)
        {
            // Добавляем класс для отображения сетки поверх фона
            _gridBackground.AddToClassList("grid-visible");

            // Важно: сетка должна быть над кастомным фоном, но под нодами
            if (_gridBackground.parent == null)
            {
                Insert(1, _gridBackground); // Вставляем после кастомного фона
                _gridBackground.StretchToParentSize();
            }
        }
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        // Обработка добавленных элементов
        if (change.edgesToCreate != null)
        {
            var edgesToRemove = new List<Edge>();
            var edgesToAdd = new List<Edge>();
            foreach (var edge in change.edgesToCreate.ToList())
            {
                if (edge.input == null || edge.output == null)
                    continue;
                // Определяем правильное направление соединения
                Port trueOutput = null;
                Port trueInput = null;
                // Если соединение уже идет в правильном направлении (output -> input)
                if (edge.output.direction == Direction.Output && edge.input.direction == Direction.Input)
                {
                    trueOutput = edge.output;
                    trueInput = edge.input;
                }
                // Если соединение идет в обратном направлении (input -> output)
                else if (edge.output.direction == Direction.Input && edge.input.direction == Direction.Output)
                {
                    trueOutput = edge.input;
                    trueInput = edge.output;
                }
                // Если оба порта одного типа - не создаем соединение
                else
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                // Если направление изменилось, создаем новое соединение с правильными портами
                if (trueOutput != edge.output || trueInput != edge.input)
                {
                    edgesToRemove.Add(edge);
                    // Проверка на возможность соединения
                    if (IsConnectionAllowed(trueOutput, trueInput))
                    {
                        // Создаем команду для создания соединения
                        var newEdge = new Edge { output = trueOutput, input = trueInput };
                        var command = new CreateConnectionCommand(this, newEdge);
                        undoManager.ExecuteCommand(command);
                        edgesToAdd.Add(newEdge);
                    }
                }
                else
                {
                    // Проверка на возможность соединения для исходного соединения
                    if (!IsConnectionAllowed(trueOutput, trueInput))
                    {
                        edgesToRemove.Add(edge);
                    }
                    else
                    {
                        var command = new CreateConnectionCommand(this, edge);
                        undoManager.ExecuteCommand(command);
                    }
                }
            }
            // Удаляем некорректные соединения
            foreach (var edge in edgesToRemove)
            {
                change.edgesToCreate.Remove(edge);
            }
            // Добавляем исправленные соединения
            if (edgesToAdd.Count > 0)
            {
                if (change.edgesToCreate == null)
                    change.edgesToCreate = new List<Edge>();
                change.edgesToCreate.AddRange(edgesToAdd);
            }
        }
        // Обработка удаленных элементов
        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove.ToList())
            {
                if (element is Edge edge)
                {
                    // Создаем команду для удаления соединения
                    var command = new DeleteEdgeCommand(this, edge);
                    undoManager.ExecuteCommand(command);
                }
                else if (element is BaseNode node)
                {
                    // Защита от удаления EntryNode
                    if (node.EntryPoint)
                    {
                        change.elementsToRemove.Remove(element);
                        EditorUtility.DisplayDialog("Cannot Delete", "The entry point node cannot be deleted.", "OK");
                        continue;
                    }
                    // Создаем команду для удаления узла
                    var command = new DeleteElementCommand(this, node);
                    undoManager.ExecuteCommand(command);
                }
            }
        }
        return change;
    }

    private bool CanEditNodeWithDoubleClick(BaseNode node)
    {
        return node is SpeechNodeText ||
               node is OptionNodeText ||
               node is DebugLogNode ||
               node is DebugWarningNode ||
               node is DebugErrorNode;
    }

    /// <summary>
    /// Проверяет, является ли узел Speech Node
    /// </summary>
    private bool IsSpeechNode(BaseNode node)
    {
        return node is SpeechNode ||
               node is SpeechNodeText ||
               node is SpeechNodeAudio ||
               node is SpeechNodeImage ||
               node is SpeechNodeRandText;
    }

    /// <summary>
    /// Проверяет, является ли узел Option Node
    /// </summary>
    private bool IsOptionNode(BaseNode node)
    {
        return node is OptionNode ||
               node is OptionNodeText ||
               node is OptionNodeAudio ||
               node is OptionNodeImage;
    }

    /// <summary>
    /// Проверяет, является ли узел обычным узлом (не Option)
    /// </summary>
    private bool IsRegularNode(BaseNode node)
    {
        return !IsOptionNode(node);
    }

    private void OnGlobalKeyDown(KeyDownEvent evt)
    {
        // Игнорируем горячие клавиши, когда открыто окно текстового редактора
        if (_activeTextEditorWindow != null)
            return;

        var settings = LoadDialogueSettings();
        bool hotkeysEnabled = settings != null && settings.General.EnableHotkeyUndoRedo;

        if (!hotkeysEnabled)
            return;

        // Ctrl + Z (Windows) или Cmd + Z (Mac) для Undo
        if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.Z && undoManager.CanUndo())
        {
            undoManager.Undo();
            evt.StopPropagation();
        }
    }

    public void DuplicateSelectedNodes()
    {
        var selectedNodes = selection.OfType<BaseNode>()
            .Where(node => !node.EntryPoint)
            .ToList();
        if (selectedNodes.Count == 0)
            return;

        var command = new DuplicateNodesCommand(this, selectedNodes);
        undoManager.ExecuteCommand(command);
    }

    private Vector2 GetMousePositionInGraphSpace()
    {
        var mousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        var worldMousePosition = contentViewContainer.WorldToLocal(new Vector2(mousePosition.x, mousePosition.y));
        return worldMousePosition;
    }

    public void UpdateGraphBackgroundInternal()
    {
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
        var graphWindows = Resources.FindObjectsOfTypeAll<DialogueGraph>();
        foreach (var window in graphWindows)
        {
            if (window.graphView != null)
                window.graphView.UpdateGraphBackgroundInternal();
        }
    }

    private void AddSearchWindow()
    {
        searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        searchWindow.Init(editorWindow, this);
        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Delete)
        {
            // существующий код удаления
            DeleteSelection();
            evt.StopPropagation();
        }
        // Новое: обработка Ctrl+D для копирования
        else if (evt.ctrlKey && evt.keyCode == KeyCode.D)
        {
            DuplicateSelectedNodes();
            evt.StopPropagation();
        }
    }

    /// <summary>
    /// Определяет поведение WireNode на основе подключенных узлов
    /// </summary>
    private bool IsWireNodeBehavingAsOptionNode(WireNode wireNode)
    {
        // Проверяем исходящие соединения
        foreach (var edge in wireNode.outputContainer[0].Query<Edge>().ToList())
        {
            var targetNode = edge.input?.node as BaseNode;
            if (targetNode != null && IsOptionNode(targetNode))
                return true;
        }

        // Проверяем входящие соединения
        foreach (var edge in wireNode.inputContainer[0].Query<Edge>().ToList())
        {
            var sourceNode = edge.output?.node as BaseNode;
            if (sourceNode != null && IsOptionNode(sourceNode))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Определяет, является ли узел "обычным" с точки зрения соединений
    /// (включая WireNode с обычным поведением)
    /// </summary>
    private bool IsRegularConnectionType(BaseNode node)
    {
        if (node is WireNode wireNode)
            return !IsWireNodeBehavingAsOptionNode(wireNode);

        return IsRegularNode(node);
    }

    /// <summary>
    /// Определяет, является ли узел "опциональным" с точки зрения соединений
    /// (включая WireNode с опциональным поведением)
    /// </summary>
    private bool IsOptionConnectionType(BaseNode node)
    {
        if (node is WireNode wireNode)
            return IsWireNodeBehavingAsOptionNode(wireNode);

        return IsOptionNode(node);
    }

    /// <summary>
    /// Проверяет, разрешено ли соединение между портами
    /// </summary>
    private bool IsConnectionAllowed(Port startPort, Port targetPort)
    {
        var startNode = startPort.node as BaseNode;
        var targetNode = targetPort.node as BaseNode;
        if (startNode == null || targetNode == null)
            return false;

        // Проверяем направление портов
        if (startPort.direction != Direction.Output || targetPort.direction != Direction.Input)
            return false;

        // Правило 1: Нельзя соединять узел сам с собой
        if (startNode == targetNode)
            return false;

        // Все остальные соединения разрешены - конфликты будут обработаны в HandleConflictingConnections
        return true;
    }

    /// <summary>
    /// Определяет совместимые порты для соединения
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        // Определяем тип стартового порта
        var startNode = startPort.node as BaseNode;
        if (startNode == null)
            return compatiblePorts;

        ports.ForEach(port =>
        {
            if (port == null || startPort == null)
                return;

            var targetNode = port.node as BaseNode;
            if (targetNode == null)
                return;

            // Пропускаем порт того же узла
            if (startPort.node == port.node)
                return;

            // Определяем правильное направление для проверки
            Port checkOutput;
            Port checkInput;

            // Если стартовый порт - output, ищем совместимые input порты
            if (startPort.direction == Direction.Output)
            {
                checkOutput = startPort;
                checkInput = port;
            }
            // Если стартовый порт - input, ищем совместимые output порты
            else if (startPort.direction == Direction.Input)
            {
                checkOutput = port;
                checkInput = startPort;
            }
            // Если оба порта одного типа - несовместимы
            else
            {
                return;
            }

            // Проверяем направление целевого порта
            if (checkOutput.direction != Direction.Output || checkInput.direction != Direction.Input)
                return;

            // Проверяем, не является ли один из узлов EntryPoint (EntryNode)
            if (startNode.EntryPoint && targetNode.EntryPoint)
                return;

            // Для входных портов Option Nodes разрешаем множественные соединения
            if (checkInput.direction == Direction.Input && IsOptionNode(targetNode))
            {
                compatiblePorts.Add(port);
                return;
            }

            // Проверка ёмкости целевого порта
            if (checkInput.direction == Direction.Input &&
                checkInput.capacity == Port.Capacity.Single &&
                checkInput.connections.Count() >= 1)
            {
                // Разрешаем замену существующих соединений для Option Nodes
                if (!IsOptionNode(targetNode))
                    return;
            }

            // Проверка ограничений на соединения узлов
            if (IsConnectionAllowed(checkOutput, checkInput))
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }

    /// <summary>
    /// Обрабатывает конфликтующие соединения при создании нового соединения
    /// </summary>
    private void HandleConflictingConnections(Edge newEdge, BaseNode startNode)
    {
        var outputPort = newEdge.output;
        var targetNode = newEdge.input.node as BaseNode;
        if (outputPort == null || targetNode == null)
            return;

        // Определяем тип нового соединения
        bool isNewConnectionOptionNode = IsOptionNode(targetNode);
        bool isNewConnectionRegularNode = !isNewConnectionOptionNode; // Любая нода, не являющаяся option

        // Получаем все существующие соединения от этого порта (кроме нового)
        var existingConnections = outputPort.connections
            .Where(e => e != newEdge)
            .ToList();

        // Случай 1: Новое соединение - обычный узел (любой, не option)
        if (isNewConnectionRegularNode)
        {
            // Удаляем все существующие соединения (включая Option)
            foreach (var edge in existingConnections.ToList())
            {
                DeleteConnection(edge);
            }
        }

        // Случай 2: Новое соединение - Option Node
        else if (isNewConnectionOptionNode)
        {
            // Проверяем, есть ли среди существующих соединений соединения с обычными узлами (не Option)
            var regularNodeConnections = existingConnections
                .Where(e => e.input != null &&
                           e.input.node != null &&
                           !IsOptionNode(e.input.node as BaseNode))
                .ToList();

            // Если есть соединения с обычными узлами, удаляем их все
            foreach (var edge in regularNodeConnections)
            {
                DeleteConnection(edge);
            }
        }

        // Случай 3: Wire Node меняет свое поведение
        else if (startNode is WireNode wireNode)
        {
            bool behavesAsOption = IsWireNodeBehavingAsOptionNode(wireNode);

            // Если Wire Node должен вести себя как обычный узел (только одно соединение)
            if (!behavesAsOption)
            {
                // Удаляем все существующие соединения кроме нового
                foreach (var edge in existingConnections.ToList())
                {
                    DeleteConnection(edge);
                }
            }
        }
    }

    /// <summary>
    /// Удаляет соединение и помечает изменения
    /// </summary>
    private void DeleteConnection(Edge edge)
    {
        if (edge == null || edge.parent == null)
            return;

        // Удаляем соединение
        edge.input?.Disconnect(edge);
        edge.output?.Disconnect(edge);
        RemoveElement(edge);
        MarkUnsavedChangeWithoutFile();
    }
    private void GenerateBlackBoard()
    {
        Blackboard = new Blackboard(this);
        Blackboard.title = "Exposed Properties";
        Blackboard.style.minWidth = 300;
        Blackboard.style.maxWidth = 400;
        Blackboard.style.minHeight = 200;
        Blackboard.style.maxHeight = 500;

        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        var intSection = new BlackboardSection { title = "Int Properties (0)" };
        var stringSection = new BlackboardSection { title = "String Properties (0)" };

        scrollView.Add(intSection);
        scrollView.Add(stringSection);

        Blackboard.Add(scrollView);

        IntSection = intSection;
        StringSection = stringSection;

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

        Blackboard.editTextRequested = (blackboard, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField)element).text;
            if (IntExposedProperties.Any(x => x.PropertyName == newValue) ||
                StringExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exists, please choose another one.", "OK");
                return;
            }

            var intPropertyIndex = IntExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            if (intPropertyIndex >= 0)
            {
                IntExposedProperties[intPropertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;
                RefreshAllPropertyNodes();
                return;
            }

            var stringPropertyIndex = StringExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            if (stringPropertyIndex >= 0)
            {
                StringExposedProperties[stringPropertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;
                RefreshAllPropertyNodes();
            }
        };

        var clearButton = new Button(ClearAllProperties)
        {
            text = "Clear All Properties"
        };
        clearButton.style.marginTop = 5;
        clearButton.style.marginLeft = 5;
        clearButton.style.marginRight = 5;
        clearButton.style.marginBottom = 5;
        Blackboard.Add(clearButton);

        Add(Blackboard);
        UpdateSectionTitles();
    }

    private BlackboardSection IntSection;
    private BlackboardSection StringSection;

    public void AddIntPropertyToBlackBoard(IntExposedProperty intProperty)
    {
        var container = new VisualElement();
        container.style.marginBottom = 5;
        var blackboardField = new BlackboardField
        {
            text = intProperty.PropertyName,
            typeText = "Int"
        };

        var nameField = new TextField("Name:") { value = intProperty.PropertyName };
        var minField = new IntegerField("Min:") { value = intProperty.MinValue };
        var maxField = new IntegerField("Max:") { value = intProperty.MaxValue };
        var valueField = new IntegerField("Value:") { value = intProperty.IntValue };

        nameField.RegisterValueChangedCallback(evt =>
        {
            intProperty.PropertyName = evt.newValue;
            blackboardField.text = evt.newValue;
            UpdateSectionTitles();
            RefreshAllPropertyNodes();
            MarkUnsavedChangeWithoutFile();
        });
        minField.RegisterValueChangedCallback(evt =>
        {
            intProperty.MinValue = evt.newValue;
            if (intProperty.IntValue < evt.newValue)
                valueField.value = evt.newValue;
            MarkUnsavedChangeWithoutFile();
        });
        maxField.RegisterValueChangedCallback(evt =>
        {
            intProperty.MaxValue = evt.newValue;
            if (intProperty.IntValue > evt.newValue)
                valueField.value = evt.newValue;
            MarkUnsavedChangeWithoutFile();
        });
        valueField.RegisterValueChangedCallback(evt =>
        {
            intProperty.IntValue = Mathf.Clamp(evt.newValue, intProperty.MinValue, intProperty.MaxValue);
            valueField.value = intProperty.IntValue;
            MarkUnsavedChangeWithoutFile();
        });

        container.Add(blackboardField);
        container.Add(nameField);
        container.Add(minField);
        container.Add(maxField);
        container.Add(valueField);

        IntSection.Add(container);

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

    public void AddStringPropertyToBlackBoard(StringExposedProperty stringProperty)
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
            MarkUnsavedChangeWithoutFile();
        });
        valueField.RegisterValueChangedCallback(evt =>
        {
            stringProperty.StringValue = evt.newValue;
            MarkUnsavedChangeWithoutFile();
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

    private void RemoveIntProperty(IntExposedProperty property, VisualElement container)
    {
        IntExposedProperties.Remove(property);

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
        MarkUnsavedChangeWithoutFile();
    }

    private void RemoveStringProperty(StringExposedProperty property, VisualElement container)
    {
        StringExposedProperties.Remove(property);

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
        MarkUnsavedChangeWithoutFile();
    }

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

    private void RefreshAllPropertyNodes()
    {
        var allPropertyNodes = nodes.ToList().OfType<IPropertyNode>();
        foreach (var node in allPropertyNodes)
        {
            node.RefreshPropertyDropdown();
        }
    }

    private void ClearAllProperties()
    {
        bool hasProperties = IntExposedProperties.Count > 0 || StringExposedProperties.Count > 0;
        if (!hasProperties)
        {
            return;
        }

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
        MarkUnsavedChangeWithoutFile();
    }

    public void ClearBlackBoardAndExposedProperties()
    {
        ClearAllProperties();
    }

    private void DeleteSelection()
    {
        var selectionCopy = selection.ToList();
        foreach (var selectedElement in selectionCopy)
        {
            var command = new DeleteElementCommand(this, selectedElement);
            undoManager.ExecuteCommand(command);
        }
    }

    public void ClearUndoRedoStacks()
    {
        undoManager.ClearStacks();
    }

    private void OnBaseCharacterChanged()
    {
        Debug.Log($"Base character changed to: {BaseCharacterGuid}");
    }

    private void ClearGraph()
    {
        var nodesToRemove = this.nodes.ToList().Where(node => !(node is EntryNode)).ToList();
        foreach (var node in nodesToRemove)
        {
            var edgesToRemove = this.edges
                .Where(e => e.input.node == node || e.output.node == node)
                .ToList();
            foreach (var edge in edgesToRemove)
                RemoveElement(edge);
            RemoveElement(node);
        }

        ClearBlackBoardAndExposedProperties();
        BaseCharacterGuid = string.Empty;
    }

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
            _hasUnsavedChangesWithoutFile = false;
            _unsavedChangesWarningShown = false;
        }
    }

    public void OpenTextEditor(string initialText, string nodeGuid, Action<string> onTextChanged)
    {
        _activeTextEditorWindow?.Close();
        _activeTextEditorWindow = null;
        ClearNodeHighlight();

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

        float viewWidth = this.layout.width;
        float viewHeight = this.layout.height;

        Vector2 targetScroll = center - new Vector2(viewWidth * 0.5f - 50f, viewHeight * 0.5f - 50f);

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

    private void OnPortPointerDown(PointerDownEvent evt)
    {
        if (evt.target is Port port && port.direction == Direction.Output)
        {
            _draggedOutputPort = port;
            port.RegisterCallback<PointerUpEvent>(OnPortPointerUp, TrickleDown.TrickleDown);
        }
    }


    private void OnPortPointerUp(PointerUpEvent evt)
    {
        if (_draggedOutputPort == null)
        {
            return;
        }

        Vector2 worldPosition = evt.position;
        Vector2 localPosition = contentViewContainer.WorldToLocal(worldPosition);
        _dragReleasePosition = localPosition;

        _draggedOutputPort.UnregisterCallback<PointerUpEvent>(OnPortPointerUp);

        var settings = LoadDialogueSettings();
        if (settings == null || !settings.General.EnableQuickNodeCreationOnDragDrop)
        {
            _draggedOutputPort = null;
            return;
        }

        // Проверяем, был ли порт отпущен на другом порту
        bool releasedOnPort = false;
        foreach (var element in graphElements)
        {
            if (element is Port port && port.direction == Direction.Input && element.worldBound.Contains(worldPosition))
            {
                releasedOnPort = true;
                break;
            }
        }

        // Проверяем, находится ли точка отпускания на другом узле
        bool releasedOnNode = false;
        foreach (var element in graphElements)
        {
            if (element is Node node && node != _draggedOutputPort.node && element.worldBound.Contains(worldPosition))
            {
                releasedOnNode = true;
                break;
            }
        }

        // Если порт не был отпущен на другом порту и не на другом узле - показываем окно создания
        if (!releasedOnPort && !releasedOnNode)
        {
            ShowFilteredNodeSearchWindow();
        }

        _draggedOutputPort = null;
    }

    private void ShowFilteredNodeSearchWindow()
    {
        var draggedOutputPort = _draggedOutputPort;
        if (draggedOutputPort?.node is not BaseNode sourceNode)
        {
            _draggedOutputPort = null;
            return;
        }

        Vector2 screenPosition = _dragReleasePosition;
        if (editorWindow != null && editorWindow.rootVisualElement != null)
        {
            Vector2 worldPosition = contentViewContainer.LocalToWorld(_dragReleasePosition);
            Vector2 rootPosition = editorWindow.rootVisualElement.WorldToLocal(worldPosition);
            Rect windowRect = editorWindow.position;
            screenPosition = new Vector2(
                windowRect.x + rootPosition.x,
                windowRect.y + rootPosition.y
            );
        }
        screenPosition += new Vector2(10, 10);

        var searchWindow = ScriptableObject.CreateInstance<FilteredNodeSearchWindow>();
        searchWindow.Init(editorWindow, this, sourceNode, (nodeType) =>
        {
            if (nodeType != null && draggedOutputPort != null)
            {
                // Если у порта уже есть соединения и его capacity = Single, удаляем существующие соединения
                if (draggedOutputPort.capacity == Port.Capacity.Single)
                {
                    foreach (var edge in draggedOutputPort.connections.ToList())
                    {
                        draggedOutputPort.Disconnect(edge);
                        edge.input?.Disconnect(edge);
                        RemoveElement(edge);
                    }
                }

                // Создаем команду для создания узла и соединения
                var command = new CreateNodeAndConnectionCommand(
                    this,
                    nodeType,
                    _dragReleasePosition,
                    draggedOutputPort
                );
                undoManager.ExecuteCommand(command);
            }

            if (_draggedOutputPort == draggedOutputPort)
            {
                _draggedOutputPort = null;
            }
        });

        SearchWindow.Open(new SearchWindowContext(screenPosition), searchWindow);
    }

    private DialogueSettingsData LoadDialogueSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueSettingsData");
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<DialogueSettingsData>(path);
    }

    public void CopySelectedNodes()
    {
        var selectedNodes = selection.OfType<BaseNode>()
            .Where(node => !node.EntryPoint && !(node is WireNode))
            .ToList();

        if (selectedNodes.Count == 0) return;

        var clipboardData = new ClipboardData();

        foreach (var node in selectedNodes)
        {
            clipboardData.nodes.Add(new SerializedNode
            {
                type = node.GetType().Name,
                guid = node.GUID,
                position = node.GetPosition().position,
                nodeData = node.SerializeNodeData()
            });
        }

        var edges = this.edges.ToList();
        foreach (var edge in edges)
        {
            if (edge.output?.node is BaseNode outputNode &&
                edge.input?.node is BaseNode inputNode &&
                selectedNodes.Contains(outputNode) &&
                selectedNodes.Contains(inputNode))
            {
                clipboardData.connections.Add(new SerializedConnection
                {
                    sourceGuid = outputNode.GUID,
                    targetGuid = inputNode.GUID,
                    portName = edge.output.portName
                });
            }
        }

        Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
        foreach (var node in selectedNodes)
        {
            var pos = node.GetPosition().position;
            minPos = new Vector2(Mathf.Min(minPos.x, pos.x), Mathf.Min(minPos.y, pos.y));
            maxPos = new Vector2(Mathf.Max(maxPos.x, pos.x), Mathf.Max(maxPos.y, pos.y));
        }

        clipboardData.center = (minPos + maxPos) / 2;
        clipboardData.size = maxPos - minPos;

        string json = JsonUtility.ToJson(clipboardData);
        GUIUtility.systemCopyBuffer = json;
        Debug.Log($"Copied {selectedNodes.Count} nodes to clipboard");
    }

    public void PasteNodesAtPosition(Vector2 position)
    {
        if (string.IsNullOrEmpty(GUIUtility.systemCopyBuffer)) return;

        try
        {
            var clipboardData = JsonUtility.FromJson<ClipboardData>(GUIUtility.systemCopyBuffer);
            if (clipboardData.nodes.Count == 0) return;

            var command = new PasteNodesCommand(this, clipboardData, position);
            undoManager.ExecuteCommand(command);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to paste nodes: {e.Message}");
        }
    }

    public void CreateNode(Type nodeType, Vector2 position)
    {
        var command = new CreateNodeCommand(this, nodeType, position);
        undoManager.ExecuteCommand(command);
    }

    [System.Serializable]
    public class ClipboardData
    {
        public List<SerializedNode> nodes = new List<SerializedNode>();
        public List<SerializedConnection> connections = new List<SerializedConnection>();
        public Vector2 center;
        public Vector2 size;
    }

    [System.Serializable]
    public class SerializedNode
    {
        public string type;
        public string guid;
        public Vector2 position;
        public string nodeData;
    }

    [System.Serializable]
    public class SerializedConnection
    {
        public string sourceGuid;
        public string targetGuid;
        public string portName;
    }
}