// Аналогично DebugLogNode, но:
// - title = "Debug Warning"
// - стиль: Resources.Load<StyleSheet>("DebugWarningNode")
// - class: "debug-warning-preview"
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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

        // Input
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Preview label (только вертикальное расширение)
        _previewLabel = new Label(MessageText)
        {
            style =
        {
        whiteSpace = WhiteSpace.Normal,
        overflow = Overflow.Visible,
        flexGrow = 0,
        flexShrink = 0,
        alignSelf = Align.Stretch,
        // ← Фиксируем максимальную ширину, как в SpeechNodeText
        maxWidth = 230f // или 240f — подбери по вкусу
        }
        };
        _previewLabel.AddToClassList("debug-log-preview"); // или warning/error

        // Кнопка редактирования
        _editButton = new Button(OpenTextEditor) { text = "✎" };
        _editButton.style.position = Position.Absolute;
        _editButton.style.top = 2;
        _editButton.style.right = 2;
        _editButton.style.width = 24;
        _editButton.style.height = 20;
        _editButton.style.fontSize = 10;

        mainContainer.Add(_previewLabel);
        titleContainer.Add(_editButton);

        // Стиль (если есть)
        var styleSheet = Resources.Load<StyleSheet>("DebugLogNode"); // или соответствующий
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        // Подписка на изменение геометрии для авто-высоты
        _previewLabel.RegisterCallback<GeometryChangedEvent>(OnPreviewLabelResized);

        RefreshExpandedState();
        RefreshPorts();
    }

    private void OnPreviewLabelResized(GeometryChangedEvent evt)
    {
        var contentHeight = _previewLabel.layout.height;
        var minHeight = 80f;
        var newHeight = Mathf.Max(minHeight, contentHeight + 60f);
        var rect = GetPosition();
        rect.height = newHeight;
        rect.width = 250f; // ← Фиксированная ширина узла
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