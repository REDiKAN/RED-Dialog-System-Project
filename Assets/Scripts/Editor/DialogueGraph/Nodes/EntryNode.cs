using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Стартовый узел - точка входа в диалоговый граф
/// Не может быть удален или перемещен
/// </summary>
public class EntryNode : BaseNode
{
    /// <summary>
    /// Инициализация стартового узла
    /// </summary>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "START";
        EntryPoint = true;

        // Создаем выходной порт
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Запрещаем удаление и перемещение
        capabilities &= ~Capabilities.Movable;
        capabilities &= ~Capabilities.Deletable;

        // Обновляем визуальное состояние узла
        RefreshExpandedState();
        RefreshPorts();

        // Добавляем специальный стиль для стартового узла
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    public override string SerializeNodeData()
    {
        return null;
    }

    public override void DeserializeNodeData(string jsonData)
    {
        // десериализация данных из JSON в узел
    }
}