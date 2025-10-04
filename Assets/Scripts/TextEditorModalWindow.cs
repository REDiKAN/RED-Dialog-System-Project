using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

public class TextEditorModalWindow : VisualElement
{
    private TextField _textField;
    private Label _guidLabel;
    private string _nodeGuid;
    private System.Action<string> _onTextChanged;

    public TextEditorModalWindow(string initialText, string nodeGuid, System.Action<string> onTextChanged)
    {
        _nodeGuid = nodeGuid;
        _onTextChanged = onTextChanged;

        // Стиль окна
        style.flexDirection = FlexDirection.Column;
        style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
        style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
        style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        style.paddingTop = style.paddingBottom = 8;
        style.paddingLeft = style.paddingRight = 8;
        style.minWidth = 400;   // ← Увеличено для удобства
        style.maxWidth = 700;
        style.minHeight = 300;  // ← Увеличено
        style.maxHeight = 600;

        // Заголовок
        var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 5 } };
        var title = new Label("Edit Text") { style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } };
        var closeButton = new Button(() => Close()) { text = "×" };
        closeButton.style.width = 24;
        closeButton.style.height = 20;
        closeButton.style.marginLeft = 10;
        header.Add(title);
        header.Add(closeButton);
        Add(header);

        // ScrollView с TextField
        var scrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            style = { flexGrow = 1, marginBottom = 10 }
        };

        _textField = new TextField()
        {
            multiline = true,
            isDelayed = true,
            // Важно: не устанавливаем maxLength, чтобы текст не обрезался
        };

        // 🔑 Ключевая настройка: включаем soft-wrap через USS-стиль
        _textField.style.whiteSpace = WhiteSpace.Normal; // ← Это включает перенос по словам

        _textField.SetValueWithoutNotify(initialText);
        _textField.RegisterValueChangedCallback(evt =>
        {
            _onTextChanged?.Invoke(evt.newValue);
        });

        scrollView.Add(_textField);
        Add(scrollView);

        // Кнопки
        var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        var copyBtn = new Button(() => EditorGUIUtility.systemCopyBuffer = _textField.value) { text = "Copy" };
        var pasteBtn = new Button(() =>
        {
            if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
            {
                _textField.value = EditorGUIUtility.systemCopyBuffer;
                _onTextChanged?.Invoke(_textField.value);
            }
        })
        { text = "Paste" };
        var clearBtn = new Button(() =>
        {
            _textField.value = "";
            _onTextChanged?.Invoke("");
        })
        { text = "Clear" };
        buttonRow.Add(copyBtn);
        buttonRow.Add(pasteBtn);
        buttonRow.Add(clearBtn);
        Add(buttonRow);

        // GUID
        _guidLabel = new Label($"Node GUID: {_nodeGuid}")
        {
            style = { fontSize = 9, color = Color.gray, marginTop = 8 }
        };
        Add(_guidLabel);
    }

    public void UpdateText(string newText)
    {
        if (_textField != null)
            _textField.SetValueWithoutNotify(newText);
    }

    public void Close()
    {
        this.RemoveFromHierarchy();
    }
}