using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Обработчик условий для диалоговых узлов
/// </summary>
public static class ConditionHandler
{
    /// <summary>
    /// Проверяет числовое условие
    /// </summary>
    /// <param name="condition">Данные узла условия</param>
    /// <param name="variables">Словарь числовых переменных</param>
    /// <returns>Результат проверки условия</returns>
    public static bool EvaluateIntCondition(IntConditionNodeData condition, Dictionary<string, int> variables)
    {
        if (!variables.TryGetValue(condition.SelectedProperty, out int value))
        {
            Debug.LogError($"Переменная {condition.SelectedProperty} не найдена в словаре");
            return false;
        }

        switch (condition.Comparison)
        {
            case ComparisonType.Equal:
                return value == condition.CompareValue;
            case ComparisonType.NotEqual:
                return value != condition.CompareValue;
            case ComparisonType.Greater:
                return value > condition.CompareValue;
            case ComparisonType.Less:
                return value < condition.CompareValue;
            case ComparisonType.GreaterOrEqual:
                return value >= condition.CompareValue;
            case ComparisonType.LessOrEqual:
                return value <= condition.CompareValue;
            default:
                return false;
        }
    }

    /// <summary>
    /// Проверяет строковое условие
    /// </summary>
    /// <param name="condition">Данные узла условия</param>
    /// <param name="variables">Словарь строковых переменных</param>
    /// <returns>Результат проверки условия</returns>
    public static bool EvaluateStringCondition(StringConditionNodeData condition, Dictionary<string, string> variables)
    {
        if (!variables.TryGetValue(condition.SelectedProperty, out string value))
        {
            Debug.LogError($"Переменная {condition.SelectedProperty} не найдена в словаре");
            return false;
        }

        switch (condition.Comparison)
        {
            case StringComparisonType.Equal:
                return value == condition.CompareValue;
            case StringComparisonType.NotEqual:
                return value != condition.CompareValue;
            case StringComparisonType.IsNullOrEmpty:
                return string.IsNullOrEmpty(value);
            default:
                return false;
        }
    }
}