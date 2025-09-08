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
        propertyDropdown.RegisterValueChangedCallback(evt =>
        {
            SelectedProperty = evt.newValue;
        });

        base.Initialize(position);
        title = "Condition (Int)";

        // Property dropdown
        propertyDropdown = new DropdownField("Property");
        propertyDropdown.choices = new List<string>(); // Инициализируем пустым списком
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
}