using System;

/// <summary>
/// Данные варианта ответа
/// </summary>
[Serializable]
public class Option
{
    public string Text; // Текст варианта
    public string NextNodeGuid; // GUID следующего узла
}