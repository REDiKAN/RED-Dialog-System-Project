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

        // Создаём компактный превью-лейбл
        _previewLabel = new Label(DialogueText)
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                overflow = Overflow.Hidden,
                maxHeight = 40, // ~2 строки
                flexGrow = 1
            }
        };
        _previewLabel.AddToClassList("speech-text-preview");

        // Кнопка редактирования
        _editButton = new Button(OpenTextEditor)
        {
            text = "✎"
        };
        _editButton.style.position = Position.Absolute;
        _editButton.style.top = 2;
        _editButton.style.right = 2;
        _editButton.style.width = 24;
        _editButton.style.height = 20;
        _editButton.style.fontSize = 10;

        // Добавляем в mainContainer
        mainContainer.Add(_previewLabel);
        titleContainer.Add(_editButton); // кнопка поверх заголовка

        // Убираем аудио-поле
        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        styleSheets.Add(Resources.Load<StyleSheet>("SpeechNodeText"));
    }

    private void OpenTextEditor()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        // Закрываем предыдущее окно, если открыто
        if (_modalWindow != null)
        {
            _modalWindow.Close();
        }

        _modalWindow = new TextEditorModalWindow(DialogueText, GUID, newText =>
        {
            DialogueText = newText;
            _previewLabel.text = DialogueText.Length > 100
                ? DialogueText.Substring(0, 100) + "..."
                : DialogueText;
        });

        // Позиционируем как Blackboard — в правом верхнем углу
        _modalWindow.style.position = Position.Absolute;
        _modalWindow.style.top = 30; // под тулбаром
        _modalWindow.style.right = 0;

        graphView.Add(_modalWindow);
    }

    public override void SetDialogueText(string text)
    {
        DialogueText = text ?? "";
        if (_previewLabel != null)
        {
            _previewLabel.text = DialogueText.Length > 100
                ? DialogueText.Substring(0, 100) + "..."
                : DialogueText;
        }
    }
}