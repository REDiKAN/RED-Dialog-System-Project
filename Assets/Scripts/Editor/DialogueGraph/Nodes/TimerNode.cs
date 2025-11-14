// Assets/Scripts/Editor/DialogueGraph/Nodes/TimerNode.cs
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

public class TimerNode : BaseNode
{
    public float DurationSeconds = 5.0f;
    private Slider _durationSlider;
    private Label _valueLabel;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Timer";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output ports
        var optionsPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        optionsPort.portName = "Options";
        outputContainer.Add(optionsPort);

        var timeoutPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        timeoutPort.portName = "Timeout";
        outputContainer.Add(timeoutPort);

        // Slider for duration
        _durationSlider = new Slider(0.1f, 60.0f)
        {
            value = DurationSeconds,
            showInputField = true
        };
        _durationSlider.RegisterValueChangedCallback(evt =>
        {
            DurationSeconds = evt.newValue;
            UpdateLabel();
        });

        // Value label
        _valueLabel = new Label();
        UpdateLabel();

        mainContainer.Add(_durationSlider);
        mainContainer.Add(_valueLabel);

        RefreshExpandedState();
        RefreshPorts();
    }

    private void UpdateLabel()
    {
        _valueLabel.text = $"{DurationSeconds:F2} sec";
    }

    public void SetDuration(float duration)
    {
        DurationSeconds = Mathf.Clamp(duration, 0.1f, 60.0f);
        _durationSlider.value = DurationSeconds;
        UpdateLabel();
    }

    [System.Serializable]
    private class TimerNodeSerializedData
    {
        public float DurationSeconds;
    }

    public override string SerializeNodeData()
    {
        var data = new TimerNodeSerializedData
        {
            DurationSeconds = DurationSeconds
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<TimerNodeSerializedData>(jsonData);
        SetDuration(data.DurationSeconds);
    }
}