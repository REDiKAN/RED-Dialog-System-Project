using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.Search;

/// <summary>
/// Узел речи NPC - содержит диалог и озвучку
/// Может иметь множество исходящих соединений к OptionNode
/// </summary>
public class SpeechNode : BaseNode
{
    public string DialogueText { get; set; } // Текст диалога
    public AudioClip AudioClip { get; set; } // Аудиофайл озвучки

    protected TextField dialogueTextField;
    protected ObjectField audioField;

    public CharacterData Speaker;
    private ObjectField speakerField;



    /// <summary>
    /// Инициализация узла речи NPC
    /// </summary>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech Node";
        DialogueText = "New Dialogue";

        // Создаем входной порт с возможностью множественных подключений
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Создаем выходной порт с возможностью множественных подключений
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Поле для текста диалога
        dialogueTextField = new TextField("Dialogue Text:");
        dialogueTextField.multiline = true;
        dialogueTextField.RegisterValueChangedCallback(evt =>
        {
            DialogueText = evt.newValue;
            //title = DialogueText.Length > 15 ? DialogueText.Substring(0, 15) + "..." : DialogueText;
        });
        dialogueTextField.SetValueWithoutNotify(DialogueText);
        mainContainer.Add(dialogueTextField);

        // Поле для выбора аудиофайла
        audioField = new ObjectField("Audio Clip");
        audioField.objectType = typeof(AudioClip);
        audioField.RegisterValueChangedCallback(evt =>
        {
            AudioClip = evt.newValue as AudioClip;
        });
        mainContainer.Add(audioField);

        speakerField = new ObjectField("Speaker");
        speakerField.objectType = typeof(CharacterData);
        speakerField.RegisterValueChangedCallback(evt =>
        {
            Speaker = evt.newValue as CharacterData;
        });
        mainContainer.Add(speakerField);

        // Обновляем визуальное состояние узла
        RefreshExpandedState();
        RefreshPorts();

        // Добавляем специальный стиль для SpeechNode
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    /// <summary>
    /// Находит порт по имени
    /// </summary>
    public Port GetPortByName(string portName)
    {
        foreach (var port in outputContainer.Children())
        {
            if (port is Port portElement && portElement.portName == portName)
            {
                return portElement;
            }
        }
        return null;
    }

    public void SetSpeaker(CharacterData speaker)
    {
        Speaker = speaker;
        if (speakerField != null)
        {
            speakerField.SetValueWithoutNotify(speaker);
        }
    }

    public virtual void SetDialogueText(string text)
    {
        DialogueText = text;
        if (dialogueTextField != null)
            dialogueTextField.SetValueWithoutNotify(text);
    }
}