using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WireNode : BaseNode
{
    public override void Initialize(Vector2 position)
    {
        title = ""; // Без заголовка
        GUID = System.Guid.NewGuid().ToString();
        SetPosition(new Rect(position, new Vector2(30, 30))); // Уменьшенный размер

        // Input (Multi)
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output (Single)
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        // Поведение
        capabilities |= Capabilities.Movable;
        capabilities |= Capabilities.Deletable;

        RefreshExpandedState();
        RefreshPorts();
    }
}