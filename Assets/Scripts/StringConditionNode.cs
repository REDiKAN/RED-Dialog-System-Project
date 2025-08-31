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
        propertyDropdown.choices = new List<string>(); // �������������� ������ �������
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

        // ����������� ���������� ������ ������� �� �������, ����� ���� ����� �������� � ����
        this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // ������ ���� �������� � ���� � ����� �������� ������ � DialogueGraphView
        RefreshPropertyDropdown();
        this.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    public void RefreshPropertyDropdown()
    {
        propertyDropdown.RegisterValueChangedCallback(evt =>
        {
            SelectedProperty = evt.newValue;
        });

        // �������� �������� �� �����
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView != null && propertyDropdown != null)
        {
            propertyDropdown.choices = graphView.StringExposedProperties
                .Where(p => p != null)
                .Select(p => p.PropertyName)
                .ToList();

            // ���� ���� ��������, �������� ������ �� ���������
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