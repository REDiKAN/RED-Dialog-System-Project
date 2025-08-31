using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class StringConditionNode : BaseConditionNode
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
        propertyDropdown.choices = new List<string>(); // Инициализируем пустым списком
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

        // Откладываем обновление списка свойств до момента, когда узел будет добавлен в граф
        this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Теперь узел добавлен в граф и может получить доступ к DialogueGraphView
        RefreshPropertyDropdown();
        this.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
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
}