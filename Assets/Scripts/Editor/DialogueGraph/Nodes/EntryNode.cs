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

        // Получаем настройки
        DialogueSettingsData settings = null;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:DialogueSettingsData");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueSettingsData>(path);
        }

        bool canMove = settings != null && settings.General.EnableEntryNodeMovement;

        // Важно: сохраняем все базовые capabilities для корректной работы с сеткой
        capabilities = Capabilities.Selectable | Capabilities.Collapsible | Capabilities.Movable;

        // Корректируем в зависимости от настройки перемещения
        if (!canMove)
        {
            capabilities &= ~Capabilities.Movable;
        }

        capabilities &= ~Capabilities.Deletable; // Запрещаем удаление всегда

        // Обновляем состояние узла
        RefreshExpandedState();
        RefreshPorts();

        // Применяем стили
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