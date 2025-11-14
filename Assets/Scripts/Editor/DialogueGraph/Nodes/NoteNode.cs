/// Assets/Scripts/Editor/DialogueGraph/Nodes/NoteNode.cs
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
    private ColorField _colorField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Note";

        // Убираем стандартные порты (но оставляем возможность удаления)
        inputContainer.Clear();
        outputContainer.Clear();

        // УБИРАЕМ эту строку - оставляем возможность удаления
        // capabilities &= ~Capabilities.Deletable;

        // Оставляем возможность сворачивания для удобства
        capabilities |= Capabilities.Collapsible;

        this.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

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
                marginBottom = 5,
                color = new Color(0.17f, 0.24f, 0.31f), // Темно-синий для лучшей читаемости
                unityFontStyleAndWeight = FontStyle.Normal
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

    private void OnPreviewLabelResized(GeometryChangedEvent evt)
    {
        // Автоматически подстраиваем высоту узла под содержимое
        var contentHeight = _previewLabel.layout.height;
        var minHeight = 120f;
        var newHeight = Mathf.Max(minHeight, contentHeight + 100f); // + отступы для цветового поля и заголовка

        var rect = GetPosition();
        rect.height = newHeight;
        rect.width = Mathf.Max(250f, rect.width); // Минимальная ширина
        SetPosition(rect);
    }

    private void OpenTextEditor()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        graphView.OpenTextEditor(NoteText, GUID, newText =>
        {
            NoteText = newText;
            _previewLabel.text = NoteText;
        });
    }

    private void UpdateBackgroundColor()
    {
        // Применяем цвет фона ко всему узлу
        style.backgroundColor = new StyleColor(BackgroundColor);

        // Автоматически настраиваем цвет текста для контрастности
        UpdateTextContrast();
    }

    private void UpdateTextContrast()
    {
        if (_previewLabel == null) return;

        // Вычисляем яркость фона для определения контрастного цвета текста
        float brightness = BackgroundColor.r * 0.299f + BackgroundColor.g * 0.587f + BackgroundColor.b * 0.114f;

        // Если фон светлый - темный текст, если темный - светлый текст
        if (brightness > 0.6f)
        {
            _previewLabel.style.color = new Color(0.17f, 0.24f, 0.31f); // Темно-синий
        }
        else
        {
            _previewLabel.style.color = new Color(0.95f, 0.95f, 0.95f); // Светлый
        }
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

    // Методы для управления визуальными связями
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

    [System.Serializable]
    private class NoteNodeSerializedData
    {
        public string NoteText;
        public Color BackgroundColor;
    }

    public override string SerializeNodeData()
    {
        var data = new NoteNodeSerializedData
        {
            NoteText = NoteText,
            BackgroundColor = BackgroundColor
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<NoteNodeSerializedData>(jsonData);
        NoteText = data.NoteText;
        BackgroundColor = data.BackgroundColor;

        // Обновление UI
        if (_previewLabel != null)
        {
            _previewLabel.text = NoteText;
        }
        if (_colorField != null)
        {
            _colorField.SetValueWithoutNotify(BackgroundColor);
        }
        UpdateBackgroundColor();
    }
}