using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

public class DialogueGrapView : GraphView
{
    public readonly Vector2 defaultNodeSize = new Vector2(150, 200);

    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();

    private NodeSearchWindow searchWindow;

    public DialogueGrapView(EditorWindow editorWindow)
    {
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

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
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(ports => 
        {
            if (startPort != ports && startPort.node != ports.node)
                compatiblePorts.Add(ports);
        });

        return compatiblePorts;
    }
    private Port GeneratePort(DialogueNode node, Direction postDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, postDirection, capacity, typeof(float));
    }
    private DialogueNode GenerateEntryPointNode()
    {
        var node = new DialogueNode
        {
            title = "Start Dialogue Point",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "Hello World !",
            EmtryPoint = true
        };

        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";

        node.outputContainer.Add(generatedPort);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        node.styleSheets.Add(Resources.Load<StyleSheet>("StartNode"));

        node.RefreshExpandedState();
        node.RefreshPorts();


        node.SetPosition(new Rect(100, 200, 100, 150));

        return node;
    }
    public void CreateNode(string nodeName, Vector2 mousePosition)
    {
        AddElement(CreateDialogueNode(nodeName, mousePosition));
    }
    public DialogueNode CreateDialogueNode(string nodeName, Vector2 position)
    {
        var dialogueNode = new DialogueNode
        {
            title = "nodeName",
            DialogueText = "nodeName",
            GUID = Guid.NewGuid().ToString(),
        };

        var inputPost = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPost.portName = "Input";
        dialogueNode.inputContainer.Add(inputPost);

        dialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));

        var button = new Button(() => { AddChoicePort(dialogueNode); });
        button.text = "New Choice";
        dialogueNode.titleContainer.Add(button);

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
    public void AddChoicePort(DialogueNode dialogueNode, string overriddenPortName = "")
    {
        var generatedPort = GeneratePort(dialogueNode, Direction.Output);

        var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(oldLabel);

        var outputPortCount = dialogueNode.outputContainer.Query("connector").ToList().Count;
        generatedPort.portName = $"Choice {outputPortCount}";

        var choicePortName = string.IsNullOrEmpty(overriddenPortName) 
            ? $"Choice {outputPortCount + 1}" 
            : overriddenPortName;

        var textField = new TextField
        {
            name = string.Empty,
            value = choicePortName
        };

        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label(" "));

        generatedPort.contentContainer.Add(textField);

        var deleteButton = new Button(() => RemovePort(dialogueNode, generatedPort))
        {
            text = "Delete"
        };
        generatedPort.contentContainer.Add(deleteButton);

        var pasteButton = new Button(() => { /* действие */ })
        {
            text = "Paste"
        };
        generatedPort.contentContainer.Add(pasteButton);

        var copyButton = new Button(() => { /* действие */ })
        {
            text = "Copy"
        };
        generatedPort.contentContainer.Add(copyButton);


        generatedPort.portName = choicePortName;

        dialogueNode.outputContainer.Add(generatedPort);

        dialogueNode.RefreshPorts();
        dialogueNode.RefreshExpandedState();
    }

    public void ClearBlackBoardAndExposedProperties()
    {
        ExposedProperties.Clear();
        Blackboard.Clear();
    }

    public void AddproperToBlackBoard(ExposedProperty exposedProperty)
    {
        var property = new ExposedProperty();
        property.PropertyName = exposedProperty.PropertyName;
        property.PropertyValue = exposedProperty.PropertyValue;

        ExposedProperties.Add(property);

        var container = new VisualElement();
        var blackgroundField = new BlackboardField { text = property.PropertyName, typeText = "string property" };

        contentContainer.Add(blackgroundField);

        var propertyValueTextField = new TextField("Value:")
        {
            value = property.PropertyValue
        };

        propertyValueTextField.RegisterValueChangedCallback(evt =>
        {
            var changigPropertyIndex = ExposedProperties.FindIndex(x => x.PropertyName == property.PropertyName);
            ExposedProperties[changigPropertyIndex].PropertyValue = evt.newValue;
        });

        var blackBoardValueRow = new BlackboardRow(blackgroundField, propertyValueTextField);
        container.Add(blackBoardValueRow);

        Blackboard.Add(container);
    }

    private void RemovePort(DialogueNode dialogueNode, Port generatedPort)
    {
        var targetEdge = edges.ToList().Where(x => x.output.portName == 
        generatedPort.portName && x.output.node == generatedPort.node);

        if (!targetEdge.Any()) return;

        var edge = targetEdge.First();

        edge.input.Disconnect(edge);

        RemoveElement(targetEdge.First());

        dialogueNode.outputContainer.Remove(generatedPort);
        dialogueNode.RefreshPorts();
        dialogueNode.RefreshExpandedState();
    }
}
