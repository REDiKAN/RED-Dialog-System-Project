using UnityEngine; // Добавляем этот using для runtime
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
/// <summary>
/// Базовый класс для всех узлов в графе диалогов
/// Содержит общие поля и методы для работы узлов
/// </summary>
public abstract class BaseNode :
#if UNITY_EDITOR
Node
#else
UnityEngine.Object
#endif
{
    public string GUID { get; set; } // Уникальный идентификатор узла
    public bool EntryPoint { get; set; } = false; // Флаг, является ли узел начальной точкой графа

    /// <summary>
    /// Инициализация узла в указанной позиции
    /// </summary>
    public virtual void Initialize(Vector2 position)
    {
        GUID = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
        SetPosition(new Rect(position, new Vector2(200, 150)));
#endif
    }

    public virtual string SerializeNodeData()
    {
        // Стандартная реализация не содержит данных
        return "{}";
    }

    public virtual void DeserializeNodeData(string jsonData)
    {
        // Стандартная реализация не содержит данных
    }
}