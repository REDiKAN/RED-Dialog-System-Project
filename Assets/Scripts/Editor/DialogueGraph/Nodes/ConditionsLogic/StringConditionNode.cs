using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using DialogueSystem;
using System;

public class StringConditionNode : BaseConditionNode, IPropertyNode
{
    public string SelectedProperty;
    public StringComparisonType Comparison;
    public string CompareValue;

    private DropdownField propertyDropdown;
    private DropdownField comparisonDropdown;
    private TextField valueField;

    public enum StringComparisonType
    {
        Equal,
        NotEqual,
        IsNullOrEmpty
    }

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Condition (String)";

        // Property dropdown
        propertyDropdown = new DropdownField("Property");
        propertyDropdown.choices = new List<string>();
        propertyDropdown.RegisterValueChangedCallback(evt =>
        {
            SelectedProperty = evt.newValue;
        });
        mainContainer.Add(propertyDropdown);

        // Comparison dropdown
        comparisonDropdown = new DropdownField("Comparison");
        comparisonDropdown.choices = System.Enum.GetNames(typeof(StringComparisonType)).ToList();
        comparisonDropdown.RegisterValueChangedCallback(evt =>
        {
            Comparison = (StringComparisonType)System.Enum.Parse(typeof(StringComparisonType), evt.newValue);
            valueField.SetEnabled(Comparison != StringComparisonType.IsNullOrEmpty);
        });
        mainContainer.Add(comparisonDropdown);

        // Value field
        valueField = new TextField("Value");
        valueField.RegisterValueChangedCallback(evt => CompareValue = evt.newValue);
        mainContainer.Add(valueField);

        // Обновление выпадающего списка свойств при добавлении узла в панель
        this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Теперь узел добавлен в граф и может получить доступ к DialogueGraphView
        RefreshPropertyDropdown();
        this.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    /// <summary>
    /// Устанавливает начальные данные узла до инициализации UI.
    /// Используется при загрузке сохранённого диалога.
    /// </summary>
    public void SetInitialData(string property, StringComparisonType comparison, string value)
    {
        SelectedProperty = property;
        Comparison = comparison;
        CompareValue = value;
    }

    public void RefreshPropertyDropdown()
    {
        propertyDropdown.RegisterValueChangedCallback(evt =>
        {
            SelectedProperty = evt.newValue;
        });

        // Получаем свойства из графа
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView != null && propertyDropdown != null)
        {
            propertyDropdown.choices = graphView.StringExposedProperties
                .Where(p => p != null)
                .Select(p => p.PropertyName)
                .ToList();

            // Если есть свойства, выбираем первое по умолчанию
            if (propertyDropdown.choices.Count > 0 && string.IsNullOrEmpty(SelectedProperty))
            {
                propertyDropdown.value = propertyDropdown.choices[0];
                SelectedProperty = propertyDropdown.choices[0];
            }
            else if (!string.IsNullOrEmpty(SelectedProperty))
            {
                propertyDropdown.value = SelectedProperty;
            }
        }
    }

    /// <summary>
    /// Обновляет UI-элементы на основе текущих значений полей.
    /// Вызывать после загрузки данных и добавления узла в граф.
    /// </summary>
    public void UpdateUIFromData()
    {
        if (propertyDropdown != null && comparisonDropdown != null && valueField != null)
        {
            propertyDropdown.value = SelectedProperty;
            comparisonDropdown.value = Comparison.ToString();
            valueField.value = CompareValue;
            valueField.SetEnabled(Comparison != StringComparisonType.IsNullOrEmpty);
        }
    }

    [System.Serializable]
    private class StringConditionNodeSerializedData
    {
        public string SelectedProperty;
        public string Comparison;
        public string CompareValue;
    }

    public override string SerializeNodeData()
    {
        var data = new StringConditionNodeSerializedData
        {
            SelectedProperty = SelectedProperty,
            Comparison = Comparison.ToString(),
            CompareValue = CompareValue
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<StringConditionNodeSerializedData>(jsonData);
        SelectedProperty = data.SelectedProperty;
        Comparison = (StringComparisonType)Enum.Parse(typeof(StringComparisonType), data.Comparison);
        CompareValue = data.CompareValue;

        // Восстановление UI
        RefreshPropertyDropdown();
        UpdateUIFromData();
    }
}