// Assets/Scripts/Editor/TextEditorModalWindow.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

public class TextEditorModalWindow : VisualElement
{
    private IMGUIContainer _imguiContainer;
    private string _text;
    private int _selectionStart;
    private int _selectionEnd;
    private Label _guidLabel;
    private string _nodeGuid;
    private Action<string> _onTextChanged;

    // === Undo/Redo система ===
    private readonly int _maxHistorySteps = 50;
    private readonly List<string> _undoStack = new List<string>();
    private readonly List<string> _redoStack = new List<string>();
    private bool _isUndoRedoOperation = false;

    public TextEditorModalWindow(string initialText, string nodeGuid, Action<string> onTextChanged)
    {
        _text = initialText ?? "";
        _nodeGuid = nodeGuid;
        _onTextChanged = onTextChanged;
        style.flexDirection = FlexDirection.Column;
        style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));
        style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
        style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        style.paddingTop = style.paddingBottom = 8;
        style.paddingLeft = style.paddingRight = 8;
        style.minWidth = 400;
        style.maxWidth = 700;
        style.minHeight = 300;
        style.maxHeight = 600;

        // Заголовок окна
        var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 5 } };
        var title = new Label("Edit Text") { style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } };
        var closeButton = new Button(() => Close()) { text = "×" };
        closeButton.style.width = 24;
        closeButton.style.height = 20;
        closeButton.style.marginLeft = 10;
        header.Add(title);
        header.Add(closeButton);
        Add(header);

        // === Панель форматирования ===
        var formatHeader = new Label("Formatting Tools")
        {
            style =
            {
                fontSize = 10,
                color = Color.gray,
                alignSelf = Align.FlexStart,
                marginBottom = 4
            }
        };
        Add(formatHeader);

        var formattingGroup = new VisualElement
        {
            style =
            {
                paddingTop = 8,
                paddingBottom = 8,
                paddingLeft = 8,
                paddingRight = 8,
                marginBottom = 8,
                flexWrap = Wrap.Wrap,
                flexDirection = FlexDirection.Row,
                alignItems = Align.FlexStart,
                justifyContent = Justify.FlexStart,
                minHeight = 36
            }
        };
        formattingGroup.style.flexGrow = 0;

        AddButton(formattingGroup, "B", "<b>", "</b>", "жирный текст");
        AddButton(formattingGroup, "I", "<i>", "</i>", "курсив");
        AddButton(formattingGroup, "S", "<s>", "</s>", "зачёркнутый");
        AddButton(formattingGroup, "U", "<u>", "</u>", "подчёркнутый");

        var btnColor = new Button(OpenColorPicker) { text = "Цвет" };
        var btnSize = new Button(OpenSizePicker) { text = "Размер" };
        var btnSprite = new Button(OpenSpritePicker) { text = "Спрайт" };
        var btnLink = new Button(OpenLinkPicker) { text = "Ссылка" };
        var btnAlign = new Button(OpenAlignPicker) { text = "Выравн." };
        var btnGradient = new Button(() => WrapSelection("<gradient>", "</gradient>", "градиент")) { text = "Град." };
        var btnOutline = new Button(() => WrapSelection("<outline>", "</outline>", "обводка")) { text = "Outline" };
        var btnSoftShadow = new Button(() => WrapSelection("<soft-shadow>", "</soft-shadow>", "тень")) { text = "Shadow" };

        var advancedButtons = new[] { btnColor, btnSize, btnSprite, btnLink, btnAlign, btnGradient, btnOutline, btnSoftShadow };
        foreach (var btn in advancedButtons)
        {
            btn.style.marginRight = 4;
            btn.style.marginBottom = 4;
            btn.style.height = 24;
            formattingGroup.Add(btn);
        }
        Add(formattingGroup);

        // --- Кнопки конвертации (отдельно) ---
        var convertToolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
        var btnToTMP = new Button(ConvertToTMP) { text = "→ TMP" };
        btnToTMP.style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.6f));
        btnToTMP.style.color = Color.white;
        btnToTMP.style.marginLeft = 4;
        var btnToMD = new Button(ConvertToMarkdown) { text = "← MD" };
        btnToMD.style.backgroundColor = new StyleColor(new Color(0.4f, 0.2f, 0.6f));
        btnToMD.style.color = Color.white;
        btnToMD.style.marginLeft = 4;
        convertToolbar.Add(btnToTMP);
        convertToolbar.Add(btnToMD);
        Add(convertToolbar);

        // IMGUI Container для текстового поля
        _imguiContainer = new IMGUIContainer(() =>
        {
            EditorGUI.BeginChangeCheck();
            var rect = GUILayoutUtility.GetRect(0, 1000, 200, 600);

            // ВАЖНО: всегда используем _text как источник истины
            var newText = EditorGUI.TextArea(rect, _text);

            // Обработка горячих клавиш
            if (Event.current.type == EventType.KeyDown && Event.current.control)
            {
                if (Event.current.keyCode == KeyCode.Z && !_isUndoRedoOperation)
                {
                    PerformUndo();
                    Event.current.Use();
                    GUI.changed = true; // ← критически важно
                    return; // ← выходим, чтобы не обрабатывать EndChangeCheck
                }
                else if (Event.current.keyCode == KeyCode.Y && !_isUndoRedoOperation)
                {
                    PerformRedo();
                    Event.current.Use();
                    GUI.changed = true;
                    return;
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                _selectionStart = editor.cursorIndex;
                _selectionEnd = editor.selectIndex;
            }

            if (EditorGUI.EndChangeCheck() && !_isUndoRedoOperation)
            {
                PushToUndoStack(_text);
                _text = newText;
                _onTextChanged?.Invoke(_text);
                _redoStack.Clear();
            }
        });
        _imguiContainer.style.flexGrow = 1;
        Add(_imguiContainer);

        // Кнопки Copy / Paste / Clear + Undo / Redo
        var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

        var undoBtn = new Button(PerformUndo) { text = "Undo" };
        var redoBtn = new Button(PerformRedo) { text = "Redo" };

        // Обновление состояния кнопок
        _imguiContainer.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            undoBtn.SetEnabled(_undoStack.Count > 0);
            redoBtn.SetEnabled(_redoStack.Count > 0);
        });

        buttonRow.Add(undoBtn);
        buttonRow.Add(redoBtn);

        var copyBtn = new Button(() => EditorGUIUtility.systemCopyBuffer = _text) { text = "Copy" };
        var pasteBtn = new Button(() =>
        {
            if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
            {
                _text = EditorGUIUtility.systemCopyBuffer;
                _onTextChanged?.Invoke(_text);
                PushToUndoStack(_text);
                _redoStack.Clear();
            }
        })
        { text = "Paste" };
        var clearBtn = new Button(() =>
        {
            _text = "";
            _onTextChanged?.Invoke("");
            PushToUndoStack("");
            _redoStack.Clear();
        })
        { text = "Clear" };

        buttonRow.Add(copyBtn);
        buttonRow.Add(pasteBtn);
        buttonRow.Add(clearBtn);
        Add(buttonRow);

        // GUID узла
        _guidLabel = new Label($"Node GUID: {_nodeGuid}") { style = { fontSize = 9, color = Color.gray, marginTop = 8 } };
        Add(_guidLabel);
    }

    private void AddButton(VisualElement parent, string label, string openTag, string closeTag, string placeholder)
    {
        var btn = new Button(() => WrapSelection(openTag, closeTag, placeholder)) { text = label };
        btn.style.marginRight = 4;
        btn.style.width = 40;
        btn.style.height = 24;
        parent.Add(btn);
    }

    private void WrapSelection(string openTag, string closeTag, string placeholder)
    {
        string selected = _text.Substring(Math.Min(_selectionStart, _selectionEnd), Math.Abs(_selectionEnd - _selectionStart));
        bool hadSelection = !string.IsNullOrEmpty(selected);
        if (!hadSelection)
        {
            selected = placeholder;
            _selectionStart = _selectionEnd;
        }
        string newText = _text.Substring(0, Math.Min(_selectionStart, _selectionEnd)) + openTag + selected + closeTag + _text.Substring(Math.Max(_selectionStart, _selectionEnd));
        _text = newText;
        int newCursorPos = Math.Min(_selectionStart, _selectionEnd) + openTag.Length;
        _selectionStart = newCursorPos;
        _selectionEnd = newCursorPos + selected.Length;
        _onTextChanged?.Invoke(_text);
        _imguiContainer.MarkDirtyRepaint();
    }

    private void OpenColorPicker()
    {
        string selected = GetSelectedOrPlaceholder("цветной текст");
        var modal = CreateModalBase();
        var colorField = new ColorField("Цвет") { value = Color.white };
        var confirm = new Button(() =>
        {
            string hex = ColorUtility.ToHtmlStringRGB(colorField.value);
            WrapSelection($"<color=#{hex}>", "</color>", selected);
            modal.RemoveFromHierarchy();
            _imguiContainer.MarkDirtyRepaint();
        })
        { text = "OK" };
        AddModalButtons(modal, confirm, () => modal.RemoveFromHierarchy());
        modal.Add(new Label("Выберите цвет:"));
        modal.Add(colorField);
        this.parent?.Add(modal);
    }

    private void OpenSizePicker()
    {
        var modal = CreateModalBase();
        var intField = new IntegerField("Значение (%)") { value = 100 };
        var confirm = new Button(() =>
        {
            WrapSelection($"<size={intField.value}%>", "</size>", "текст");
            modal.RemoveFromHierarchy();
            _imguiContainer.MarkDirtyRepaint();
        })
        { text = "OK" };
        AddModalButtons(modal, confirm, () => modal.RemoveFromHierarchy());
        modal.Add(new Label("Введите размер (%):"));
        modal.Add(intField);
        this.parent?.Add(modal);
    }

    private void OpenSpritePicker()
    {
        var modal = CreateModalBase();
        var textField = new TextField("Имя спрайта") { value = "my_sprite" };
        var confirm = new Button(() =>
        {
            if (!string.IsNullOrWhiteSpace(textField.value))
            {
                string tag = $"<sprite name=\"{textField.value.Trim()}\">";
                string newText = _text.Substring(0, _selectionStart) + tag + _text.Substring(_selectionEnd);
                _text = newText;
                _selectionStart = _selectionEnd = _selectionStart + tag.Length;
                _onTextChanged?.Invoke(_text);
            }
            modal.RemoveFromHierarchy();
            _imguiContainer.MarkDirtyRepaint();
        })
        { text = "OK" };
        AddModalButtons(modal, confirm, () => modal.RemoveFromHierarchy());
        modal.Add(new Label("Введите имя спрайта:"));
        modal.Add(textField);
        this.parent?.Add(modal);
    }

    private void OpenLinkPicker()
    {
        var modal = CreateModalBase();
        var textField = new TextField("URL") { value = "https://example.com" };
        var confirm = new Button(() =>
        {
            if (!string.IsNullOrWhiteSpace(textField.value))
            {
                string url = textField.value.Trim();
                string openTag = $"<link=\"{url}\"><u>";
                string closeTag = "</u></link>";
                WrapSelection(openTag, closeTag, "ссылка");
            }
            modal.RemoveFromHierarchy();
            _imguiContainer.MarkDirtyRepaint();
        })
        { text = "OK" };
        AddModalButtons(modal, confirm, () => modal.RemoveFromHierarchy());
        modal.Add(new Label("Введите URL ссылки:"));
        modal.Add(textField);
        this.parent?.Add(modal);
    }

    private void OpenAlignPicker()
    {
        var modal = CreateModalBase();
        var choices = new List<string> { "left", "center", "right", "justified" };
        var dropdown = new DropdownField(choices, 0);
        var confirm = new Button(() =>
        {
            string align = choices[dropdown.index];
            WrapSelection($"<align={align}>", "</align>", "выровненный текст");
            modal.RemoveFromHierarchy();
            _imguiContainer.MarkDirtyRepaint();
        })
        { text = "OK" };
        var cancel = new Button(() => modal.RemoveFromHierarchy()) { text = "Отмена" };
        var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
        btnRow.Add(confirm);
        btnRow.Add(cancel);
        modal.Add(new Label("Выберите выравнивание:"));
        modal.Add(dropdown);
        modal.Add(btnRow);
        this.parent?.Add(modal);
    }

    private string GetSelectedOrPlaceholder(string placeholder)
    {
        string selected = _text.Substring(Math.Min(_selectionStart, _selectionEnd), Math.Abs(_selectionEnd - _selectionStart));
        if (string.IsNullOrEmpty(selected))
        {
            selected = placeholder;
            _selectionStart = _selectionEnd;
        }
        return selected;
    }

    private VisualElement CreateModalBase()
    {
        var modal = new VisualElement();
        modal.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
        modal.style.paddingTop = 10;
        modal.style.paddingBottom = 10;
        modal.style.paddingLeft = 10;
        modal.style.paddingRight = 10;
        modal.style.marginTop = 10;
        modal.style.marginBottom = 10;
        modal.style.borderTopLeftRadius = 4;
        modal.style.borderTopRightRadius = 4;
        modal.style.borderBottomLeftRadius = 4;
        modal.style.borderBottomRightRadius = 4;
        return modal;
    }

    private void AddModalButtons(VisualElement modal, Button confirm, Action onCancel)
    {
        var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
        btnRow.Add(confirm);
        btnRow.Add(new Button(() => { onCancel(); }) { text = "Отмена" });
        modal.Add(btnRow);
    }

    public void UpdateText(string newText)
    {
        _text = newText ?? "";
        _imguiContainer.MarkDirtyRepaint();
    }

    public void Close()
    {
        _undoStack.Clear();
        _redoStack.Clear();

        this.RemoveFromHierarchy();
    }
    private void ConvertToTMP()
    {
        if (string.IsNullOrEmpty(_text)) return;
        string tmpText = MarkdownToTMP.Convert(_text);
        _text = tmpText;
        _onTextChanged?.Invoke(_text);
        _imguiContainer.MarkDirtyRepaint();
    }

    private void ConvertToMarkdown()
    {
        if (string.IsNullOrEmpty(_text)) return;
        string markdown = TMPToMarkdown.Convert(_text);
        _text = markdown;
        _onTextChanged?.Invoke(_text);
        _imguiContainer.MarkDirtyRepaint();
    }

    // === Undo/Redo вспомогательные методы ===

    private void PushToUndoStack(string state)
    {
        _undoStack.Add(state);
        if (_undoStack.Count > _maxHistorySteps)
            _undoStack.RemoveAt(0);
    }

    private void PerformUndo()
    {
        if (_undoStack.Count == 0) return;

        _isUndoRedoOperation = true;
        _redoStack.Add(_text);
        _text = _undoStack[_undoStack.Count - 1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _onTextChanged?.Invoke(_text);
        _isUndoRedoOperation = false;

        _imguiContainer.MarkDirtyRepaint();

        _imguiContainer.MarkDirtyRepaint();
        GUI.changed = true;
    }

    private void PerformRedo()
    {
        if (_redoStack.Count == 0) return;

        _isUndoRedoOperation = true;
        _undoStack.Add(_text);
        _text = _redoStack[_redoStack.Count - 1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        _onTextChanged?.Invoke(_text);
        _isUndoRedoOperation = false;

        _imguiContainer.MarkDirtyRepaint();

        _imguiContainer.MarkDirtyRepaint();
        GUI.changed = true;
    }
}