using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ���������� ������� ��� ���������� �����
/// </summary>
public static class ConditionHandler
{
    /// <summary>
    /// ��������� �������� �������
    /// </summary>
    /// <param name="condition">������ ���� �������</param>
    /// <param name="variables">������� �������� ����������</param>
    /// <returns>��������� �������� �������</returns>
    public static bool EvaluateIntCondition(IntConditionNodeData condition, Dictionary<string, int> variables)
    {
        if (!variables.TryGetValue(condition.SelectedProperty, out int value))
        {
            Debug.LogError($"���������� {condition.SelectedProperty} �� ������� � �������");
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
    /// ��������� ��������� �������
    /// </summary>
    /// <param name="condition">������ ���� �������</param>
    /// <param name="variables">������� ��������� ����������</param>
    /// <returns>��������� �������� �������</returns>
    public static bool EvaluateStringCondition(StringConditionNodeData condition, Dictionary<string, string> variables)
    {
        if (!variables.TryGetValue(condition.SelectedProperty, out string value))
        {
            Debug.LogError($"���������� {condition.SelectedProperty} �� ������� � �������");
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