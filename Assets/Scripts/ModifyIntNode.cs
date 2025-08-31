using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModifyIntNode : BaseNode, IPropertyNode
{
    public string SelectedProperty;
    public OperatorType Operator;
    public int Value;

    private DropdownField propertyDropdown;
    private DropdownField operatorDropdown;
    private IntegerField valueField;

    public enum OperatorType
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Divide,
        Increment,
        Decrement
    }

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Modify Int";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        // Property dropdown
        propertyDropdown = new DropdownField("Property");
        propertyDropdown.choices = new List<string>();
        propertyDropdown.RegisterValueChangedCallback(evt =>
        {
            SelectedProperty = evt.newValue;
        });
        mainContainer.Add(propertyDropdown);

        // Operator dropdown
        operatorDropdown = new DropdownField("Operator");
        operatorDropdown.choices = System.Enum.GetNames(typeof(OperatorType)).ToList();
        operatorDropdown.RegisterValueChangedCallback(evt =>
        {
            Operator = (OperatorType)System.Enum.Parse(typeof(OperatorType), evt.newValue);
            UpdateValueFieldVisibility();
        });
        mainContainer.Add(operatorDropdown);

        // Value field
        valueField = new IntegerField("Value");
        valueField.RegisterValueChangedCallback(evt => Value = evt.newValue);
        mainContainer.Add(valueField);

        UpdateValueFieldVisibility();

        // Откладываем обновление списка свойств до момента, когда узел будет добавлен в граф
        this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

        RefreshExpandedState();
        RefreshPorts();
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        // Теперь узел добавлен в граф и может получить доступ к DialogueGraphView
        RefreshPropertyDropdown();
        this.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
    }

    private void UpdateValueFieldVisibility()
    {
        valueField.style.display = (Operator == OperatorType.Increment ||
                                  Operator == OperatorType.Decrement) ?
                                  DisplayStyle.None : DisplayStyle.Flex;
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
            else if (!string.IsNullOrEmpty(SelectedProperty) &&
                     propertyDropdown.choices.Contains(SelectedProperty))
            {
                propertyDropdown.value = SelectedProperty;
            }
            else if (!string.IsNullOrEmpty(SelectedProperty))
            {
                // Если свойство было удалено, сбрасываем выбор
                propertyDropdown.value = "";
                SelectedProperty = "";
            }
        }
    }
}