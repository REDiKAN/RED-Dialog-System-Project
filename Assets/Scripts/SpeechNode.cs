using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.Search;
using UnityEngine;

/// <summary>
/// ���� ���� NPC - �������� ������ � �������
/// ����� ����� ��������� ��������� ���������� � OptionNode
/// </summary>
public class SpeechNode : BaseNode
{
    public string DialogueText { get; set; } // ����� �������
    public AudioClip AudioClip { get; set; } // ��������� �������

    private TextField dialogueTextField;
    private ObjectField audioField;

    /// <summary>
    /// ������������� ���� ���� NPC
    /// </summary>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech Node";
        DialogueText = "New Dialogue";

        // ������� ������� ���� � ������������ ������������� �����������
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // ������� �������� ���� � ������������ ������������� �����������
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // ���� ��� ������ �������
        dialogueTextField = new TextField("Dialogue Text:");
        dialogueTextField.multiline = true;
        dialogueTextField.RegisterValueChangedCallback(evt =>
        {
            DialogueText = evt.newValue;
            title = DialogueText.Length > 15 ? DialogueText.Substring(0, 15) + "..." : DialogueText;
        });
        dialogueTextField.SetValueWithoutNotify(DialogueText);
        mainContainer.Add(dialogueTextField);

        // ���� ��� ������ ����������
        audioField = new ObjectField("Audio Clip");
        audioField.objectType = typeof(AudioClip);
        audioField.RegisterValueChangedCallback(evt =>
        {
            AudioClip = evt.newValue as AudioClip;
        });
        mainContainer.Add(audioField);

        // ��������� ���������� ��������� ����
        RefreshExpandedState();
        RefreshPorts();

        // ��������� ����������� ����� ��� SpeechNode
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    /// <summary>
    /// ��������� �������������� �������� ���� ��� ����������� OptionNode
    /// </summary>
    private void AddOutputPort()
    {
        var newOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        newOutputPort.portName = $"Option {outputContainer.childCount}";

        // ���� ��� �������������� ����� �����
        var textField = new TextField { value = newOutputPort.portName };
        textField.RegisterValueChangedCallback(evt => newOutputPort.portName = evt.newValue);

        // ������� ����������� ����� � ��������� ���� ��� ��������������
        newOutputPort.contentContainer.RemoveAt(0);
        newOutputPort.contentContainer.Add(new Label("  "));
        newOutputPort.contentContainer.Add(textField);

        outputContainer.Add(newOutputPort);
        RefreshExpandedState();
        RefreshPorts();
    }
}
