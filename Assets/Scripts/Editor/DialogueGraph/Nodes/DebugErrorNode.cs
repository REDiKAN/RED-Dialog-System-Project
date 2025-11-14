using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class DebugErrorNode : BaseNode
{
    public Label _previewLabel;
    private Button _editButton;
    public string MessageText { get; set; } = "";

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Debug Error";

        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        this.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

        RefreshExpandedState();
        RefreshPorts();
    }

    private void OnAttachedToPanel(AttachToPanelEvent evt)
    {
        _previewLabel = new Label(MessageText)
        {
            style =
        {
            whiteSpace = WhiteSpace.Normal,
            overflow = Overflow.Visible,
            flexGrow = 0,
            flexShrink = 0,
            alignSelf = Align.Stretch,
            maxWidth = 230f
        }
        };
        _previewLabel.AddToClassList("debug-error-preview");
        mainContainer.Add(_previewLabel);
        mainContainer.AddToClassList("main-container"); // ← важно для обводки

        _editButton = new Button(OpenTextEditor) { text = "✎" };
        _editButton.style.position = Position.Absolute;
        _editButton.style.top = 2;
        _editButton.style.right = 2;
        _editButton.style.width = 24;
        _editButton.style.height = 20;
        _editButton.style.fontSize = 10;
        titleContainer.Add(_editButton);

        var styleSheet = Resources.Load<StyleSheet>("DebugErrorNode");
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        _previewLabel.RegisterCallback<GeometryChangedEvent>(OnPreviewLabelResized);
    }

    private void OnPreviewLabelResized(GeometryChangedEvent evt)
    {
        var contentHeight = _previewLabel.layout.height;
        var minHeight = 80f;
        var newHeight = Mathf.Max(minHeight, contentHeight + 60f);
        var rect = GetPosition();
        rect.height = newHeight;
        rect.width = 250f;
        SetPosition(rect);
    }

    private void OpenTextEditor()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        graphView.OpenTextEditor(MessageText, GUID, newText =>
        {
            MessageText = newText;
            _previewLabel.text = MessageText;
        });
    }

    public override string SerializeNodeData()
    {
        return null;
    }

    public override void DeserializeNodeData(string jsonData)
    {
        // десериализация данных из JSON в узел
    }
}