using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.Search;
using UnityEngine;

/// <summary>
/// Узел речи NPC - содержит диалог и озвучку
/// Может иметь множество исходящих соединений к OptionNode
/// </summary>
public class SpeechNode : BaseNode
{
    public string DialogueText { get; set; } // Текст диалога
    public AudioClip AudioClip { get; set; } // Аудиофайл озвучки

    private TextField dialogueTextField;
    private ObjectField audioField;

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
            title = DialogueText.Length > 15 ? DialogueText.Substring(0, 15) + "..." : DialogueText;
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

        // Обновляем визуальное состояние узла
        RefreshExpandedState();
        RefreshPorts();

        // Добавляем специальный стиль для SpeechNode
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    /// <summary>
    /// Добавляет дополнительный выходной порт для подключения OptionNode
    /// </summary>
    private void AddOutputPort()
    {
        var newOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        newOutputPort.portName = $"Option {outputContainer.childCount}";

        // Поле для редактирования имени порта
        var textField = new TextField { value = newOutputPort.portName };
        textField.RegisterValueChangedCallback(evt => newOutputPort.portName = evt.newValue);

        // Убираем стандартный лейбл и добавляем поле для редактирования
        newOutputPort.contentContainer.RemoveAt(0);
        newOutputPort.contentContainer.Add(new Label("  "));
        newOutputPort.contentContainer.Add(textField);

        outputContainer.Add(newOutputPort);
        RefreshExpandedState();
        RefreshPorts();
    }
}
