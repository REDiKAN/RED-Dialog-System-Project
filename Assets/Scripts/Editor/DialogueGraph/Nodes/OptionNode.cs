using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.Search;

/// <summary>
/// ���� �������� ������ ������ - �������� ����� ������ � �������
/// ����� ���� ��������� ������ � SpeechNode
/// </summary>
public class OptionNode : BaseNode
{
    public string ResponseText { get; set; } // ����� ������
    public AudioClip AudioClip { get; set; } // ��������� �������

    protected TextField responseTextField;
    protected ObjectField audioField;

    /// <summary>
    /// ������������� ���� �������� ������ ������
    /// </summary>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Option Node";
        ResponseText = "New Response";

        // ������� ������� ���� (������ ���� �����������)
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // ������� �������� ���� (������ ���� �����������)
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // ���� ��� ������ ������
        responseTextField = new TextField("Response Text:");
        responseTextField.multiline = true;
        responseTextField.RegisterValueChangedCallback(evt =>
        {
            ResponseText = evt.newValue;
            title = ResponseText.Length > 15 ? ResponseText.Substring(0, 15) + "..." : ResponseText;
        });
        responseTextField.SetValueWithoutNotify(ResponseText);
        mainContainer.Add(responseTextField);

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

        // ��������� ����������� ����� ��� OptionNode
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }
}