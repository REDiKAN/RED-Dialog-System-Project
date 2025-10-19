using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

public class PauseNode : BaseNode
{
    public float DurationSeconds = 1.0f;
    private Slider _durationSlider;
    private Label _valueLabel;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Pause";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Slider with input field
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
}
