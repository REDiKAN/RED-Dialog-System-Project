using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

public class DebugLogNode : BaseNode
{
    public Label _previewLabel;
    private Button _editButton;
    public string MessageText { get; set; } = "";

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Debug Log";

        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        this.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

        RefreshExpandedState();
        RefreshPorts();

        SetupDoubleClickHandler();
    }

    private void SetupDoubleClickHandler()
    {
        this.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        this.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);

        // Помечаем интерактивные элементы, которые не должны реагировать на двойной клик
        if (_editButton != null)
            _editButton.AddToClassList("no-double-click");

        // Помечаем порты подключения
        MarkPortsAsNonClickable();
    }

    private void MarkPortsAsNonClickable()
    {
        foreach (var port in inputContainer.Children().OfType<Port>())
            port.AddToClassList("no-double-click");

        foreach (var port in outputContainer.Children().OfType<Port>())
            port.AddToClassList("no-double-click");
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.clickCount != 2) return;

        var target = evt.target as VisualElement;
        while (target != null)
        {
            if (target.ClassListContains("no-double-click"))
                return;
            target = target.parent;
        }

        OpenTextEditor();
        evt.StopPropagation();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        var target = evt.target as VisualElement;
        while (target != null)
        {
            if (target.ClassListContains("no-double-click"))
            {
                this.style.cursor = StyleKeyword.Auto;
                return;
            }
            target = target.parent;
        }
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
        _previewLabel.AddToClassList("debug-log-preview");
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

        var styleSheet = Resources.Load<StyleSheet>("DebugLogNode");
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
}