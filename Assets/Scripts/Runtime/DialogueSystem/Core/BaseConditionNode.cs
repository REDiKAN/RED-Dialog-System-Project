using UnityEngine; // Добавляем этот using для runtime
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
public abstract class BaseConditionNode : BaseNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
#if UNITY_EDITOR
        // Создание входного порта
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Создание выходных портов True и False
        var truePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        truePort.portName = "True";
        outputContainer.Add(truePort);

        var falsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        falsePort.portName = "False";
        outputContainer.Add(falsePort);
#endif
    }
}