using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

public class EndNode : BaseNode
{
    public string NextDialogueName { get; set; } = ""; // Имя следующего диалога

    private TextField nextDialogueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "End Node";

        // Входной порт
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Поле для ввода имени следующего диалога
        nextDialogueField = new TextField("Next Dialogue:");
        nextDialogueField.RegisterValueChangedCallback(evt =>
        {
            NextDialogueName = evt.newValue;
        });
        mainContainer.Add(nextDialogueField);

        RefreshExpandedState();
        RefreshPorts();

        // Стиль для EndNode
        styleSheets.Add(Resources.Load<StyleSheet>("EndNode"));
    }
}
