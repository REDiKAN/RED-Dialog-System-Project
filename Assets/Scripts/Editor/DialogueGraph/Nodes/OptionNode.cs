using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.Search;

/// <summary>
/// Узел варианта ответа игрока - содержит текст ответа и озвучку
/// Может быть подключен только к SpeechNode
/// </summary>
public class OptionNode : BaseNode
{
    public string ResponseText { get; set; } // Текст ответа
    public AudioClip AudioClip { get; set; } // Аудиофайл озвучки

    protected TextField responseTextField;
    protected ObjectField audioField;

    /// <summary>
    /// Инициализация узла варианта ответа игрока
    /// </summary>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Option Node";
        ResponseText = "New Response";

        // Создаем входной порт (только одно подключение)
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Создаем выходной порт (только одно подключение)
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Поле для текста ответа
        responseTextField = new TextField("Response Text:");
        responseTextField.multiline = true;
        responseTextField.RegisterValueChangedCallback(evt =>
        {
            ResponseText = evt.newValue;
            //title = ResponseText.Length > 15 ? ResponseText.Substring(0, 15) + "..." : ResponseText;
        });
        responseTextField.SetValueWithoutNotify(ResponseText);
        mainContainer.Add(responseTextField);

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

        // Добавляем специальный стиль для OptionNode
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    public virtual void SetResponseText(string text)
    {
        ResponseText = text;
        if (responseTextField != null)
            responseTextField.SetValueWithoutNotify(text);
    }
}