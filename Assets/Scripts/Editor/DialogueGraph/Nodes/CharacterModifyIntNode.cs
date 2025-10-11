using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using DialogueSystem;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CharacterModifyIntNode : BaseNode
{
    public string CharacterName = "";
    public string SelectedVariable = "";
    public OperatorType Operator;
    public int Value;

    private TextField characterNameField;
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

        characterNameField = new TextField("Character Name");
        characterNameField.RegisterValueChangedCallback(evt =>
        {
            CharacterName = evt.newValue;
            RefreshVariableDropdown();
        });
        mainContainer.Add(characterNameField);

        variableDropdown = new DropdownField("Variable") { choices = new List<string>() };
        variableDropdown.RegisterValueChangedCallback(evt => SelectedVariable = evt.newValue);
        mainContainer.Add(variableDropdown);

        var choices = System.Enum.GetNames(typeof(OperatorType));
        operatorDropdown = new DropdownField(choices.ToList(), choices[0]); // choices[0] = "Set"
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
        characterNameField?.SetValueWithoutNotify(CharacterName);
        RefreshVariableDropdown();
        variableDropdown?.SetValueWithoutNotify(SelectedVariable);
        operatorDropdown?.SetValueWithoutNotify(Operator.ToString());
        valueField.value = Value;
    }

    private void RefreshVariableDropdown()
    {
        var choices = new List<string>();
        if (!string.IsNullOrEmpty(CharacterName))
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:CharacterData {CharacterName}");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var charData = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (charData != null)
                    choices = charData.Variables.Select(v => v.VariableName).ToList();
            }
#endif
        }
        variableDropdown.choices = choices;
        if (!string.IsNullOrEmpty(SelectedVariable) && choices.Contains(SelectedVariable))
            variableDropdown.value = SelectedVariable;
        else if (choices.Count > 0)
            variableDropdown.value = choices[0];
    }
}