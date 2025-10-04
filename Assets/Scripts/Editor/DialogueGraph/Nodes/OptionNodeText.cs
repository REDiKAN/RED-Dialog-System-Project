// Assets/Scripts/Editor/DialogueGraph/Nodes/OptionNodeText.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class OptionNodeText : OptionNode
{
    private Label _previewLabel;
    private Button _editButton;
    private TextEditorModalWindow _modalWindow;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Option (Text)";

        // Удаляем стандартное текстовое поле
        if (responseTextField != null)
        {
            mainContainer.Remove(responseTextField);
            responseTextField = null;
        }

        // Превью
        _previewLabel = new Label(ResponseText)
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                overflow = Overflow.Hidden,
                maxHeight = 40,
                flexGrow = 1
            }
        };
        _previewLabel.AddToClassList("option-text-preview");

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

        mainContainer.Add(_previewLabel);
        titleContainer.Add(_editButton);

        // Убираем аудио-поле
        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        styleSheets.Add(Resources.Load<StyleSheet>("OptionNodeText"));
    }

    private void OpenTextEditor()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        if (_modalWindow != null)
        {
            _modalWindow.Close();
        }

        _modalWindow = new TextEditorModalWindow(ResponseText, GUID, newText =>
        {
            ResponseText = newText;
            _previewLabel.text = ResponseText.Length > 100 
                ? ResponseText.Substring(0, 100) + "..." 
                : ResponseText;
        });

        _modalWindow.style.position = Position.Absolute;
        _modalWindow.style.top = 30;
        _modalWindow.style.right = 0;

        graphView.Add(_modalWindow);
    }

    public override void SetResponseText(string text)
    {
        ResponseText = text ?? "";
        if (_previewLabel != null)
        {
            _previewLabel.text = ResponseText.Length > 100 
                ? ResponseText.Substring(0, 100) + "..." 
                : ResponseText;
        }
    }
}