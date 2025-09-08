using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class BaseConditionNode : BaseNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);

        // Создаем входной порт
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Создаем выходные порты True и False
        var truePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        truePort.portName = "True";
        outputContainer.Add(truePort);

        var falsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        falsePort.portName = "False";
        outputContainer.Add(falsePort);
    }
}
