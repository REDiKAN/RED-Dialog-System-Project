using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class DebugWarningNode : BaseNode
{
    public Label _previewLabel;
    private Button _editButton;
    private TextEditorModalWindow _modalWindow;
    public string MessageText { get; set; } = "";

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Debug Warning";

        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        this.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        this.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

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
        _previewLabel.AddToClassList("debug-warning-preview");
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

        var styleSheet = Resources.Load<StyleSheet>("DebugWarningNode");
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        _previewLabel.RegisterCallback<GeometryChangedEvent>(OnPreviewLabelResized);
    }

    private void OnDetachedFromPanel(DetachFromPanelEvent evt)
    {
        if (_modalWindow != null)
        {
            _modalWindow.Close();
            _modalWindow = null;
        }
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
        if (_modalWindow != null) _modalWindow.Close();
        _modalWindow = new TextEditorModalWindow(MessageText, GUID, newText =>
        {
            MessageText = newText;
            _previewLabel.text = MessageText;
        });
        _modalWindow.style.position = Position.Absolute;
        _modalWindow.style.top = 30;
        _modalWindow.style.right = 0;
        graphView.Add(_modalWindow);
    }
}