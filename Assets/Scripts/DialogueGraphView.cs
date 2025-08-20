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
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();

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
    /// ���������, ��������� �� ���������� ����� ����� �������
    /// SpeechNode ����� ��������� ������ � OptionNode, OptionNode ����� ��������� ������ � SpeechNode
    /// </summary>
    private bool IsConnectionAllowed(Port startPort, Port targetPort)
    {
        var startNode = startPort.node as BaseNode;
        var targetNode = targetPort.node as BaseNode;

        // ���������� ����������� ����������
        if (startPort.direction == Direction.Output)
        {
            // ���������� �� startNode � targetNode

            // SpeechNode ����� ��������� ������ � OptionNode
            if (startNode is SpeechNode)
            {
                return targetNode is OptionNode;
            }

            // OptionNode ����� ��������� ������ � SpeechNode
            if (startNode is OptionNode)
            {
                return targetNode is SpeechNode;
            }

            // EntryNode ����� ��������� ������ � SpeechNode
            if (startNode is EntryNode)
            {
                return targetNode is SpeechNode;
            }
        }
        else if (startPort.direction == Direction.Input)
        {
            // ���������� �� targetNode � startNode

            // SpeechNode ����� ��������� ������ � OptionNode ��� EntryNode
            if (startNode is SpeechNode)
            {
                return targetNode is OptionNode || targetNode is EntryNode;
            }

            // OptionNode ����� ��������� ������ � SpeechNode
            if (startNode is OptionNode)
            {
                return targetNode is SpeechNode;
            }
        }

        return false;
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
        Blackboard.Add(new BlackboardSection { title = "Exposed Properties" });

        // ���������� ���������� ������ ��������
        Blackboard.addItemRequested = blackboard =>
        {
            AddPropertyToBlackBoard(new ExposedProperty());
        };

        // ���������� �������������� ����� ��������
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

        // ��������� ������ ����� � ����
        Add(Blackboard);
    }

    /// <summary>
    /// ��������� �������� �� ������ �����
    /// </summary>
    public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
    {
        var localProperty = new ExposedProperty
        {
            PropertyName = exposedProperty.PropertyName,
            PropertyValue = exposedProperty.PropertyValue
        };

        ExposedProperties.Add(localProperty);

        // ������� ��������� ��� ��������
        var container = new VisualElement();
        var blackboardField = new BlackboardField
        {
            text = localProperty.PropertyName,
            typeText = "String"
        };

        // ���� ��� �������� ��������
        var propertyValueTextField = new TextField("Value:")
        {
            value = localProperty.PropertyValue
        };

        // ���������� ��������� �������� ��������
        propertyValueTextField.RegisterValueChangedCallback(evt =>
        {
            var changingPropertyIndex = ExposedProperties.FindIndex(x => x.PropertyName == localProperty.PropertyName);
            ExposedProperties[changingPropertyIndex].PropertyValue = evt.newValue;
        });

        // ������� ������ ��� ����������� ��������
        var blackboardValueRow = new BlackboardRow(blackboardField, propertyValueTextField);
        container.Add(blackboardValueRow);

        // ��������� �������� �� ������ �����
        Blackboard.Add(container);
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
        ExposedProperties.Clear();
        Blackboard.Clear();
    }
}