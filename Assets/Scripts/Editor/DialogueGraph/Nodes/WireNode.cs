using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WireNode : BaseNode
{
    public override void Initialize(Vector2 position)
    {
        title = ""; // Ѕез заголовка
        GUID = System.Guid.NewGuid().ToString();
        SetPosition(new Rect(position, new Vector2(30, 30))); // ”меньшенный размер

        // Input (Multi)
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output (Single)
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        // ѕоведение
        capabilities |= Capabilities.Movable;
        capabilities |= Capabilities.Deletable;

        RefreshExpandedState();
        RefreshPorts();
    }

    public override string SerializeNodeData()
    {
        // WireNode не имеет дополнительных данных
        return "{}";
    }

    public override void DeserializeNodeData(string jsonData)
    {
        // WireNode не имеет дополнительных данных дл€ десериализации
    }
}