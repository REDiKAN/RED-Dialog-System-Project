using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

/// <summary>
/// Ѕазовый класс дл€ всех узлов диалогового графа
/// —одержит общую логику и свойства дл€ всех узлов
/// </summary>
public abstract class BaseNode : Node
{
    public string GUID { get; set; } // ”никальный идентификатор узла
    public bool EntryPoint { get; set; } = false; // явл€етс€ ли узел точкой входа

    /// <summary>
    /// »нициализаци€ узла с указанной позицией
    /// </summary>
    public virtual void Initialize(Vector2 position)
    {
        GUID = Guid.NewGuid().ToString();
        SetPosition(new Rect(position, new Vector2(200, 150)));
    }
}
