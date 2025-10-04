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

        if (responseTextField != null)
        {
            mainContainer.Remove(responseTextField);
            responseTextField = null;
        }

        _previewLabel = new Label(ResponseText)
        {
            style =
            {
                whiteSpace = WhiteSpace.Normal,
                overflow = Overflow.Visible,
                flexGrow = 1,
                flexShrink = 0,
                alignSelf = Align.Stretch
            }
        };
        _previewLabel.AddToClassList("option-text-preview");

        _editButton = new Button(OpenTextEditor) { text = "✎" };
        _editButton.style.position = Position.Absolute;
        _editButton.style.top = 2;
        _editButton.style.right = 2;
        _editButton.style.width = 24;
        _editButton.style.height = 20;
        _editButton.style.fontSize = 10;

        mainContainer.Add(_previewLabel);
        titleContainer.Add(_editButton);

        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        styleSheets.Add(Resources.Load<StyleSheet>("OptionNodeText"));

        _previewLabel.RegisterCallback<GeometryChangedEvent>(OnPreviewLabelResized);
    }

    private void OnPreviewLabelResized(GeometryChangedEvent evt)
    {
        var contentHeight = _previewLabel.layout.height;
        var minHeight = 80f;
        var newHeight = Mathf.Max(minHeight, contentHeight + 60);

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

        _modalWindow = new TextEditorModalWindow(ResponseText, GUID, newText =>
        {
            ResponseText = newText;
            _previewLabel.text = ResponseText;
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
            _previewLabel.text = ResponseText;
    }
}