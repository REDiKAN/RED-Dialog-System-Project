// Assets/Scripts/Editor/DialogueGraph/Utilities/TextEditorModalWindow.cs
using UnityEngine;
using UnityEngine.UIElements;
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

        // Стиль окна (как у Blackboard)
        style.flexDirection = FlexDirection.Column;
        style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));

        // Границы: задаём по сторонам
        style.borderTopWidth = 1;
        style.borderBottomWidth = 1;
        style.borderLeftWidth = 1;
        style.borderRightWidth = 1;

        style.borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        style.borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));

        style.paddingTop = 8;
        style.paddingBottom = 8;
        style.paddingLeft = 8;
        style.paddingRight = 8;
        style.minWidth = 300;
        style.maxWidth = 500;
        style.minHeight = 200;
        style.maxHeight = 400;

        // Заголовок
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 5;

        var title = new Label("Edit Text") { style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } };
        var closeButton = new Button(() => Close()) { text = "×" };
        closeButton.style.width = 24;
        closeButton.style.height = 20;
        closeButton.style.marginLeft = 10;

        header.Add(title);
        header.Add(closeButton);
        Add(header);

        // ScrollView с TextField
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1;
        scrollView.style.marginBottom = 10;

        _textField = new TextField()
        {
            multiline = true,
            isDelayed = true
        };
        _textField.SetValueWithoutNotify(initialText);
        _textField.RegisterValueChangedCallback(evt =>
        {
            _onTextChanged?.Invoke(evt.newValue);
        });
        scrollView.Add(_textField);
        Add(scrollView);

        // Кнопки: Copy, Paste, Clear
        var buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.justifyContent = Justify.SpaceBetween;

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

        // GUID внизу
        _guidLabel = new Label($"Node GUID: {_nodeGuid}")
        {
            style =
            {
                fontSize = 9,
                color = Color.gray,
                marginTop = 8
            }
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