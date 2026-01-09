using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

public class OptionNodeText : OptionNode
{
    private Label _previewLabel;
    private Button _editButton;

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

        SetupDoubleClickHandler();
    }


    private void SetupDoubleClickHandler()
    {
        this.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        this.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);

        // Помечаем интерактивные элементы, которые не должны реагировать на двойной клик
        if (_editButton != null)
            _editButton.AddToClassList("no-double-click");

        // Помечаем порты подключения
        MarkPortsAsNonClickable();
    }

    private void MarkPortsAsNonClickable()
    {
        foreach (var port in inputContainer.Children().OfType<Port>())
            port.AddToClassList("no-double-click");

        foreach (var port in outputContainer.Children().OfType<Port>())
            port.AddToClassList("no-double-click");
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.clickCount != 2) return;

        var target = evt.target as VisualElement;
        while (target != null)
        {
            if (target.ClassListContains("no-double-click"))
                return;
            target = target.parent;
        }

        OpenTextEditor();
        evt.StopPropagation();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        var target = evt.target as VisualElement;
        while (target != null)
        {
            if (target.ClassListContains("no-double-click"))
            {
                this.style.cursor = StyleKeyword.Auto;
                return;
            }
            target = target.parent;
        }
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

        graphView.OpenTextEditor(ResponseText, GUID, newText =>
        {
            ResponseText = newText;
            _previewLabel.text = ResponseText;
        });
    }

    public override void SetResponseText(string text)
    {
        ResponseText = text ?? "";
        if (_previewLabel != null)
            _previewLabel.text = ResponseText;
    }

    [System.Serializable]
    private class OptionNodeTextSerializedData
    {
        public string ResponseText;
    }

    public override string SerializeNodeData()
    {
        var data = new OptionNodeTextSerializedData
        {
            ResponseText = ResponseText
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<OptionNodeTextSerializedData>(jsonData);
        ResponseText = data.ResponseText;

        // Обновление UI
        if (_previewLabel != null)
        {
            _previewLabel.text = ResponseText;
        }
    }
}