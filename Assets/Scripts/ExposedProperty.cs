using UnityEngine;

/// <summary>
/// Свойство для черной доски - переменная, которую можно использовать в диалогах
/// </summary>
[System.Serializable]
public class ExposedProperty
{
    public string PropertyName = "New Property"; // Имя свойства
    public string PropertyValue = "New Value";   // Значение свойства
}