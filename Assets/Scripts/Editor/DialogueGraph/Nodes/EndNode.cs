using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

public class EndNode : BaseNode
{
    public string NextDialogueName { get; set; } = ""; // ��� ���������� �������

    private TextField nextDialogueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "End Node";

        // ������� ����
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // ���� ��� ����� ����� ���������� �������
        nextDialogueField = new TextField("Next Dialogue:");
        nextDialogueField.RegisterValueChangedCallback(evt =>
        {
            NextDialogueName = evt.newValue;
        });
        mainContainer.Add(nextDialogueField);

        RefreshExpandedState();
        RefreshPorts();

        // ����� ��� EndNode
        styleSheets.Add(Resources.Load<StyleSheet>("EndNode"));
    }
}
