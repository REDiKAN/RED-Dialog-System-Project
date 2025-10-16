// Assets/Scripts/Editor/DialogueGraph/Nodes/NoteNode.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class NoteNode : BaseNode
{
    public string NoteText { get; set; } = "";
    public Color BackgroundColor { get; set; } = new Color(1f, 0.98f, 0.77f, 1f);
    public List<string> ConnectedNodeGuids { get; set; } = new List<string>();

    private Label _previewLabel;
    private Button _editButton;
    private TextEditorModalWindow _modalWindow;
    private ColorField _colorField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Note";

        // Убираем стандартные порты
        inputContainer.Clear();
        outputContainer.Clear();

        // Убираем возможность соединения
        capabilities &= ~Capabilities.Deletable;
        capabilities &= ~Capabilities.Collapsible;

        this.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        this.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

        RefreshExpandedState();
        RefreshPorts();
    }

    private void OnAttachedToPanel(AttachToPanelEvent evt)
    {
        // Создаем превью текста
        _previewLabel = new Label(NoteText)
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                overflow = Overflow.Visible,
                flexGrow = 1,
                flexShrink = 0,
                alignSelf = Align.Stretch,
                fontSize = 11,
                unityTextAlign = TextAnchor.UpperLeft,
                marginTop = 5,
                marginBottom = 5
            }
        };
        _previewLabel.AddToClassList("note-preview");
        mainContainer.Add(_previewLabel);

        // Кнопка редактирования
        _editButton = new Button(OpenTextEditor) { text = "✎" };
        _editButton.style.position = Position.Absolute;
        _editButton.style.top = 2;
        _editButton.style.right = 2;
        _editButton.style.width = 24;
        _editButton.style.height = 20;
        _editButton.style.fontSize = 10;
        titleContainer.Add(_editButton);

        // Поле выбора цвета
        _colorField = new ColorField("Background Color");
        _colorField.value = BackgroundColor;
        _colorField.showAlpha = true;
        _colorField.RegisterValueChangedCallback(evt =>
        {
            BackgroundColor = evt.newValue;
            UpdateBackgroundColor();
        });
        mainContainer.Add(_colorField);

        // Загружаем стили
        var styleSheet = Resources.Load<StyleSheet>("NoteNode");
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        UpdateBackgroundColor();

        // Подписываемся на изменение размера текста
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
        // Автоматически подстраиваем высоту узла под содержимое
        var contentHeight = _previewLabel.layout.height;
        var minHeight = 100f;
        var newHeight = Mathf.Max(minHeight, contentHeight + 80f); // + отступы для цветового поля

        var rect = GetPosition();
        rect.height = newHeight;
        rect.width = Mathf.Max(250f, rect.width); // Минимальная ширина
        SetPosition(rect);
    }

    private void OpenTextEditor()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        if (_modalWindow != null)
            _modalWindow.Close();

        _modalWindow = new TextEditorModalWindow(NoteText, GUID, newText =>
        {
            NoteText = newText;
            _previewLabel.text = NoteText;
        });
        _modalWindow.style.position = Position.Absolute;
        _modalWindow.style.top = 30;
        _modalWindow.style.right = 0;
        graphView.Add(_modalWindow);
    }

    private void UpdateBackgroundColor()
    {
        // Применяем цвет фона ко всему узлу
        style.backgroundColor = new StyleColor(BackgroundColor);
    }

    public void SetNoteText(string text)
    {
        NoteText = text ?? "";
        if (_previewLabel != null)
            _previewLabel.text = NoteText;
    }

    public void SetBackgroundColor(Color color)
    {
        BackgroundColor = color;
        if (_colorField != null)
            _colorField.value = color;
        UpdateBackgroundColor();
    }

    public void AddVisualConnection(string targetNodeGuid)
    {
        if (!ConnectedNodeGuids.Contains(targetNodeGuid))
            ConnectedNodeGuids.Add(targetNodeGuid);
    }

    public void RemoveVisualConnection(string targetNodeGuid)
    {
        ConnectedNodeGuids.Remove(targetNodeGuid);
    }

    public void ClearVisualConnections()
    {
        ConnectedNodeGuids.Clear();
    }
}