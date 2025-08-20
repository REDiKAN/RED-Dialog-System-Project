using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

public class DialogueNode : BaseNode
{
    public string DialogueText { get; set; }
    private TextField textField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Dialogue Node";
        DialogueText = "New Dialogue";

        // Добавляем входной порт
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Добавляем выходной порт
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Текстовое поле для диалога
        textField = new TextField(string.Empty);
        textField.multiline = true;
        textField.RegisterValueChangedCallback(evt =>
        {
            DialogueText = evt.newValue;
            title = DialogueText.Length > 15 ? DialogueText.Substring(0, 15) + "..." : DialogueText;
        });
        textField.SetValueWithoutNotify(DialogueText);
        mainContainer.Add(textField);

        RefreshExpandedState();
        RefreshPorts();
    }
}
