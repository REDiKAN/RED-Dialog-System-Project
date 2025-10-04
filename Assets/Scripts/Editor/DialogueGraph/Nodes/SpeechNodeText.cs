// Assets/Scripts/Editor/DialogueGraph/Nodes/SpeechNodeText.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class SpeechNodeText : SpeechNode
{
    private Label _previewLabel;
    private Button _editButton;
    private TextEditorModalWindow _modalWindow;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech (Text)";

        // Удаляем стандартное текстовое поле
        if (dialogueTextField != null)
        {
            mainContainer.Remove(dialogueTextField);
            dialogueTextField = null;
        }

        // Создаём превью-лейбл с поддержкой роста по высоте
        _previewLabel = new Label(DialogueText)
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                overflow = Overflow.Visible, // ← Важно: не Hidden!
                flexGrow = 1,
                flexShrink = 0,
                alignSelf = Align.Stretch
            }
        };
        _previewLabel.AddToClassList("speech-text-preview");

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

        // Убираем аудио-поле
        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        styleSheets.Add(Resources.Load<StyleSheet>("SpeechNodeText"));

        // Подписываемся на изменение геометрии, чтобы обновлять высоту узла
        _previewLabel.RegisterCallback<GeometryChangedEvent>(OnPreviewLabelResized);
    }

    private void OnPreviewLabelResized(GeometryChangedEvent evt)
    {
        // Обновляем высоту узла на основе содержимого
        var contentHeight = _previewLabel.layout.height;
        var minHeight = 80f; // минимум для заголовка + отступов
        var newHeight = Mathf.Max(minHeight, contentHeight + 60); // + отступы и кнопка

        var rect = GetPosition();
        rect.height = newHeight;
        SetPosition(rect);
    }

    private void OpenTextEditor()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        if (_modalWindow != null)
            _modalWindow.Close();

        _modalWindow = new TextEditorModalWindow(DialogueText, GUID, newText =>
        {
            DialogueText = newText;
            _previewLabel.text = DialogueText;
        });
        _modalWindow.style.position = Position.Absolute;
        _modalWindow.style.top = 30;
        _modalWindow.style.right = 0;
        graphView.Add(_modalWindow);
    }

    public override void SetDialogueText(string text)
    {
        DialogueText = text ?? "";
        if (_previewLabel != null)
            _previewLabel.text = DialogueText;
    }
}