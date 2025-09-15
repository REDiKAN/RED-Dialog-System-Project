using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

/// <summary>
/// ���� ��� �������������� �������� � ������������� �� ���������� �����
/// ������������ ���������� ����������� SpeechNode � OptionNode
/// </summary>
public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(250, 300);
    public Blackboard Blackboard;
    public string BaseCharacterGuid;

    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();

    public List<IntExposedProperty> IntExposedProperties = new List<IntExposedProperty>();
    public List<StringExposedProperty> StringExposedProperties = new List<StringExposedProperty>();

    private EditorWindow editorWindow;
    private NodeSearchWindow searchWindow;

    public DialogueGraphView(EditorWindow editorWindow)
    {
        this.editorWindow = editorWindow;

        // ��������� ����� ��� �����
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));

        // ����������� ���������������
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // ��������� ������������ ��� ����������� � ���������
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // ��������� ����� � �������� ����
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // ������� ��������� ����
        AddElement(NodeFactory.CreateEntryNode(new Vector2(100, 200)));

        // ��������� ���� ������ �����
        AddSearchWindow();

        // ������� ������ ����� ��� �������
        GenerateBlackBoard();

        // ������������ ���������� ������� ������ ��� �������� �����
        this.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    /// <summary>
    /// ��������� ���� ������ ����� � ����
    /// </summary>
    private void AddSearchWindow()
    {
        searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        searchWindow.Init(editorWindow, this);

        // ����������� �������� ����� ����� ���� ������
        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    /// <summary>
    /// ���������� ������� ������ ��� �������� �����
    /// </summary>
    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Delete)
        {
            // ���������, ���� �� � ��������� EntryNode
            if (selection.OfType<BaseNode>().Any(node => node.EntryPoint))
            {
                EditorUtility.DisplayDialog("Cannot Delete", "The entry point node cannot be deleted.", "OK");
                evt.StopPropagation();
                return;
            }

            // ������� ��������� ��������
            DeleteSelection();
            evt.StopPropagation();
        }
    }

    /// <summary>
    /// ���������� ����������� ����� ��� ���������� � ������ �����������
    /// SpeechNode ����� ��������� ������ � OptionNode, OptionNode ����� ��������� ������ � SpeechNode
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        // ���������� ��� ����� � �����
        ports.ForEach(port =>
        {
            // ���������� ��� �� ����, ���� ���� �� ���� � ����� � ��� �� ������������
            if (startPort != port &&
                startPort.node != port.node &&
                startPort.direction != port.direction)
            {
                // ��������� ����������� �� ���������� �����
                if (IsConnectionAllowed(startPort, port))
                {
                    compatiblePorts.Add(port);
                }
            }
        });

        return compatiblePorts;
    }

    /// <summary>
    /// ���������, ��������� �� ���������� ����� �������
    /// SpeechNode ����� ����������� ������ � OptionNode, OptionNode ����� ����������� ������ � SpeechNode
    /// EndNode ����� ����������� ������ � OptionNode
    /// </summary>
    private bool IsConnectionAllowed(Port startPort, Port targetPort)
    {
        var startNode = startPort.node as BaseNode;
        var targetNode = targetPort.node as BaseNode;

        if (startPort.direction == Direction.Output)
        {
            return (startNode, targetNode) switch
            {
                (SpeechNode, OptionNode) => true,
                (SpeechNode, IntConditionNode) => true,
                (SpeechNode, StringConditionNode) => true,
                (OptionNode, SpeechNode) => true,
                (OptionNode, IntConditionNode) => true,
                (OptionNode, StringConditionNode) => true,
                (OptionNode, EndNode) => true, // ��������� ����������� �� OptionNode � EndNode
                (EntryNode, SpeechNode) => true,
                (ModifyIntNode, SpeechNode) => true,
                (ModifyIntNode, OptionNode) => true,
                (ModifyIntNode, IntConditionNode) => true,
                (ModifyIntNode, StringConditionNode) => true,
                (IntConditionNode, OptionNode) => IsConditionNodeConnectedToSpeech(startNode as IntConditionNode),
                (IntConditionNode, SpeechNode) => IsConditionNodeConnectedToOption(startNode as IntConditionNode),
                (StringConditionNode, OptionNode) => IsConditionNodeConnectedToSpeech(startNode as StringConditionNode),
                (StringConditionNode, SpeechNode) => IsConditionNodeConnectedToOption(startNode as StringConditionNode),
                (_, EndNode) => false, // ��������� ����������� � EndNode �� ����� ������ �����
                _ => false
            };
        }
        else
        {
            return (startNode, targetNode) switch
            {
                (SpeechNode, OptionNode) => true,
                (SpeechNode, IntConditionNode) => true,
                (SpeechNode, StringConditionNode) => true,
                (OptionNode, SpeechNode) => true,
                (OptionNode, IntConditionNode) => true,
                (OptionNode, StringConditionNode) => true,
                (EndNode, OptionNode) => true, // ��������� ����������� �� EndNode � OptionNode (������� ����)
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
    /// ���������, ��������� �� ���� ������� � SpeechNode
    /// </summary>
    private bool IsConditionNodeConnectedToSpeech(BaseConditionNode conditionNode)
    {
        if (conditionNode == null) return false;

        var inputPort = conditionNode.inputContainer.Children().FirstOrDefault() as Port;
        return inputPort != null && inputPort.connections.Any(edge =>
            edge.output.node is SpeechNode);
    }

    /// <summary>
    /// ���������, ��������� �� ���� ������� � OptionNode
    /// </summary>
    private bool IsConditionNodeConnectedToOption(BaseConditionNode conditionNode)
    {
        if (conditionNode == null) return false;

        var inputPort = conditionNode.inputContainer.Children().FirstOrDefault() as Port;
        return inputPort != null && inputPort.connections.Any(edge =>
            edge.output.node is OptionNode);
    }

    /// <summary>
    /// ������� ���� ���������� ���� � �������� �������
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
    /// ������� ������ ����� ��� exposed properties
    /// </summary>
    private void GenerateBlackBoard()
    {
        Blackboard = new Blackboard(this);
        Blackboard.title = "Exposed Properties";

        // ������� ��������� ������ ��� ������ ����� �������
        var intSection = new BlackboardSection { title = "Int Properties" };
        var stringSection = new BlackboardSection { title = "String Properties" };

        Blackboard.Add(intSection);
        Blackboard.Add(stringSection);

        // ���������� ���������� ������ ��������
        Blackboard.addItemRequested = blackboard =>
        {
            // ������� ���� ��� ������ ���� ��������
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Int Property"), false, () =>
                AddPropertyToBlackBoard(new IntExposedProperty()));
            menu.AddItem(new GUIContent("Add String Property"), false, () =>
                AddPropertyToBlackBoard(new StringExposedProperty()));
            menu.ShowAsContext();
        };

        // ���������� �������������� ����� �������� (������ ��� ������ �������, ���� ��� ��� ������������)
        Blackboard.editTextRequested = (blackboard, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField)element).text;

            // ��������� ��� ���� ������� �� ������������ �����
            if (IntExposedProperties.Any(x => x.PropertyName == newValue) ||
                StringExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exists, please choose another one.", "OK");
                return;
            }

            // ���� �������� � Int ���������
            var intPropertyIndex = IntExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            if (intPropertyIndex >= 0)
            {
                IntExposedProperties[intPropertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;
                return;
            }

            // ���� �������� � String ���������
            var stringPropertyIndex = StringExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            if (stringPropertyIndex >= 0)
            {
                StringExposedProperties[stringPropertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;
            }
        };

        // ��������� ������ ����� � ����
        Add(Blackboard);
    }


    /// <summary>
    /// ��������� �������� �� ������ �����
    /// </summary>
    // <summary>
    /// ��������� �������� �� ������ �����
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

            // ���� ��� �������������� int ��������
            var nameField = new TextField("Name:") { value = intProperty.PropertyName };
            var minField = new IntegerField("Min:") { value = intProperty.MinValue };
            var maxField = new IntegerField("Max:") { value = intProperty.MaxValue };
            var valueField = new IntegerField("Value:") { value = intProperty.IntValue };

            // ����������� ���������
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

            Blackboard[0].Add(container); // ��������� � ������ int �������

            blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete", action =>
                {
                // ������� ������������� ��������
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

                    // ������ �������������
                    if (EditorUtility.DisplayDialog("Confirm Delete",
                        $"Property '{intProperty.PropertyName}' is used in {usageCount} nodes.\nDelete anyway?",
                        "Delete", "Cancel"))
                    {
                        // ������� �� ������
                        IntExposedProperties.Remove(intProperty);

                        // ��������� ��� ����, ������� ������������ ��� ��������
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

                        // ��������� ��� ���� � ����������� ��������
                        var allPropertyNodes = nodes.ToList().OfType<IPropertyNode>();
                        foreach (var node in allPropertyNodes)
                        {
                            node.RefreshPropertyDropdown();
                        }

                        // ������� � ������� ���������� ������� �� ����� ��������
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

            Blackboard[1].Add(container); // ��������� � ������ string �������

            // ��������� ����������� ���� ��� string �������
            blackboardField.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete", action =>
                {
                    // ������� ������������� ��������
                    int usageCount = 0;
                    var conditionNodes = nodes.ToList().OfType<StringConditionNode>();
                    foreach (var node in conditionNodes)
                    {
                        if (node.SelectedProperty == stringProperty.PropertyName)
                            usageCount++;
                    }

                    // ������ �������������
                    if (EditorUtility.DisplayDialog("Confirm Delete",
                        $"Property '{stringProperty.PropertyName}' is used in {usageCount} condition nodes.\nDelete anyway?",
                        "Delete", "Cancel"))
                    {
                        // ������� �� ������
                        StringExposedProperties.Remove(stringProperty);

                        // ��������� ��� ����, ������� ������������ ��� ��������
                        foreach (var node in nodes.ToList().OfType<StringConditionNode>())
                        {
                            if (node.SelectedProperty == stringProperty.PropertyName)
                            {
                                node.SelectedProperty = "";
                                Debug.LogError($"Error: Variable {stringProperty.PropertyName} was removed but is still used in StringConditionNode {node.GUID}");
                            }
                        }

                        // ��������� ��� ���� � ����������� ��������
                        var allPropertyNodes = nodes.ToList().OfType<IPropertyNode>();
                        foreach (var node in allPropertyNodes)
                        {
                            node.RefreshPropertyDropdown();
                        }

                        // ������� � ������� ���������� ������� �� ����� ��������
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
    /// ������� ��������� �������� �� �����
    /// </summary>
    private void DeleteSelection()
    {
        // ������� ����� ��������� ��� ����������� ��������
        var selectionCopy = selection.ToList();

        foreach (var selectedElement in selectionCopy)
        {
            if (selectedElement is BaseNode node)
            {
                // ������� ��������� �����
                var edgesToRemove = edges.ToList().Where(e => e.input.node == node || e.output.node == node).ToList();
                foreach (var edge in edgesToRemove)
                {
                    RemoveElement(edge);
                }

                // ������� ����
                RemoveElement(node);
            }
            else if (selectedElement is Edge edge)
            {
                // ������� �����
                RemoveElement(edge);
            }
        }
    }

    // ������� ���� ����� � ����� DialogueGraphView
    /// <summary>
    /// ������� ������� ������ �����
    /// </summary>
    public void ClearBlackBoardAndExposedProperties()
    {
        IntExposedProperties.Clear();
        StringExposedProperties.Clear();
        Blackboard.Clear();

        // ��������� ������� ������ ����� �������
        Blackboard.Add(new BlackboardSection { title = "Int Properties" });
        Blackboard.Add(new BlackboardSection { title = "String Properties" });
    }
}