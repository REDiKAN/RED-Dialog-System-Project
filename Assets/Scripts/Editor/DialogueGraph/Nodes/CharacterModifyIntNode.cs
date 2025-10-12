using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.Search;
using DialogueSystem;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CharacterModifyIntNode : BaseNode
{
    public CharacterData CharacterAsset;
    public string SelectedVariable = "";
    public OperatorType Operator;
    public int Value;

    private ObjectField characterField;
    private DropdownField variableDropdown;
    private DropdownField operatorDropdown;
    private IntegerField valueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Character Modify Int";

        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        characterField = new ObjectField("Character")
        {
            objectType = typeof(CharacterData)
        };
        characterField.RegisterValueChangedCallback(evt =>
        {
            CharacterAsset = evt.newValue as CharacterData;
            RefreshVariableDropdown();
        });
        mainContainer.Add(characterField);

        variableDropdown = new DropdownField("Variable") { choices = new List<string>() };
        variableDropdown.RegisterValueChangedCallback(evt => SelectedVariable = evt.newValue);
        mainContainer.Add(variableDropdown);

        var choices = System.Enum.GetNames(typeof(OperatorType));
        operatorDropdown = new DropdownField(choices.ToList(), choices[0]);
        operatorDropdown.label = "Operator";
        operatorDropdown.RegisterValueChangedCallback(evt =>
        {
            Operator = (OperatorType)System.Enum.Parse(typeof(OperatorType), evt.newValue);
            UpdateValueFieldVisibility();
        });
        mainContainer.Add(operatorDropdown);

        valueField = new IntegerField("Value");
        valueField.RegisterValueChangedCallback(evt => Value = evt.newValue);
        mainContainer.Add(valueField);

        UpdateValueFieldVisibility();
        RefreshExpandedState();
        RefreshPorts();
    }

    private void UpdateValueFieldVisibility()
    {
        valueField.style.display = (Operator == OperatorType.Increment || Operator == OperatorType.Decrement)
            ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void UpdateUIFromData()
    {
        characterField?.SetValueWithoutNotify(CharacterAsset);
        RefreshVariableDropdown();
        variableDropdown?.SetValueWithoutNotify(SelectedVariable);
        operatorDropdown?.SetValueWithoutNotify(Operator.ToString());
        valueField.value = Value;
    }

    private void RefreshVariableDropdown()
    {
        var choices = new List<string>();
        if (CharacterAsset != null)
        {
            choices = CharacterAsset.Variables.Select(v => v.VariableName).ToList();
        }
        variableDropdown.choices = choices;
        if (!string.IsNullOrEmpty(SelectedVariable) && choices.Contains(SelectedVariable))
            variableDropdown.value = SelectedVariable;
        else if (choices.Count > 0)
            variableDropdown.value = choices[0];
    }
}