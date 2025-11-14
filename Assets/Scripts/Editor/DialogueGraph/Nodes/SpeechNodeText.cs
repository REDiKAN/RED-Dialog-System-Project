// Assets/Scripts/Editor/DialogueGraph/Nodes/SpeechNodeText.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class SpeechNodeText : SpeechNode
{
    private Label _previewLabel;
    private Button _editButton;

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

        SetupDoubleClickHandler();
    }


    private void SetupDoubleClickHandler()
    {
        this.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        this.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);

        // Помечаем интерактивные элементы, которые не должны реагировать на двойной клик
        if (_editButton != null)
            _editButton.AddToClassList("no-double-click");
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        // Проверяем, что это двойной клик
        if (evt.clickCount != 2) return;

        // Проверяем, что клик не попал на интерактивные элементы
        var target = evt.target as VisualElement;
        while (target != null)
        {
            if (target.ClassListContains("no-double-click"))
                return;
            target = target.parent;
        }

        // Открываем редактор текста
        OpenTextEditor();

        // Предотвращаем распространение события
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
        if (graphView == null)
        {
            Debug.LogWarning("SpeechNodeText: DialogueGraphView not found. Cannot open text editor.");
            return;
        }

        graphView.OpenTextEditor(DialogueText, GUID, newText =>
        {
            DialogueText = newText;
            _previewLabel.text = DialogueText;
        });
    }

    public override void SetDialogueText(string text)
    {
        DialogueText = text ?? "";
        if (_previewLabel != null)
            _previewLabel.text = DialogueText;
    }

    [System.Serializable]
    private class SpeechNodeTextData
    {
        public string DialogueText;
        public string SpeakerGuid;
        // Другие необходимые поля
    }

    [System.Serializable]
    private class SpeechNodeTextSerializedData
    {
        public string DialogueText;
        public string SpeakerGuid;
    }

    public override string SerializeNodeData()
    {
        string speakerGuid = string.Empty;
        if (Speaker != null)
        {
            speakerGuid = AssetDatabaseHelper.GetAssetGuid(Speaker);
        }

        var data = new SpeechNodeTextSerializedData
        {
            DialogueText = DialogueText,
            SpeakerGuid = speakerGuid
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<SpeechNodeTextSerializedData>(jsonData);
        DialogueText = data.DialogueText;

        // Загрузка спикера по GUID
        if (!string.IsNullOrEmpty(data.SpeakerGuid))
        {
            Speaker = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(data.SpeakerGuid);
        }

        // Обновление UI
        _previewLabel.text = DialogueText;
        if (speakerField != null)
        {
            speakerField.SetValueWithoutNotify(Speaker);
        }
    }
}