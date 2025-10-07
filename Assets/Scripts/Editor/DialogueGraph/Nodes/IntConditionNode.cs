using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using DialogueSystem;

public class IntConditionNode : BaseConditionNode, IPropertyNode
{
    public string SelectedProperty;
    public ComparisonType Comparison;
    public int CompareValue;

    private DropdownField propertyDropdown;
    private DropdownField comparisonDropdown;
    private IntegerField valueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Condition (Int)";

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
        comparisonDropdown.choices = System.Enum.GetNames(typeof(ComparisonType)).ToList();
        comparisonDropdown.RegisterValueChangedCallback(evt =>
        {
            Comparison = (ComparisonType)System.Enum.Parse(typeof(ComparisonType), evt.newValue);
        });
        mainContainer.Add(comparisonDropdown);

        // Value field
        valueField = new IntegerField("Value");
        valueField.RegisterValueChangedCallback(evt => CompareValue = evt.newValue);
        mainContainer.Add(valueField);

        // Загрузка свойств при добавлении в панель
        this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Теперь узел добавлен в граф и может получить доступ к DialogueGraphView
        RefreshPropertyDropdown();
        this.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }
    public void SetInitialData(string property, ComparisonType comparison, int value)
    {
        SelectedProperty = property;
        Comparison = comparison;
        CompareValue = value;
    }
    public void RefreshPropertyDropdown()
    {
        // Получаем свойства из графа
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView != null && propertyDropdown != null)
        {
            propertyDropdown.choices = graphView.IntExposedProperties
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
        }
    }
}