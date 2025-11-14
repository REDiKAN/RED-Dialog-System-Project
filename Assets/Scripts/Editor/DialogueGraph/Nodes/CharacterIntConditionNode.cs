using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using DialogueSystem;
using UnityEditor;
using System;

public class CharacterIntConditionNode : BaseConditionNode
{
    public string CharacterName = "";
    public string SelectedVariable = "";
    public ComparisonType Comparison;
    public int CompareValue;

    private TextField characterNameField;
    private DropdownField variableDropdown;
    private DropdownField comparisonDropdown;
    private IntegerField valueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Character Condition (Int)";

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

        var choices = System.Enum.GetNames(typeof(ComparisonType));
        comparisonDropdown = new DropdownField(choices.ToList(), choices[0]); // "Equal"
        comparisonDropdown.label = "Comparison";

        comparisonDropdown.RegisterValueChangedCallback(evt =>
            Comparison = (ComparisonType)System.Enum.Parse(typeof(ComparisonType), evt.newValue));
        mainContainer.Add(comparisonDropdown);

        valueField = new IntegerField("Value");
        valueField.RegisterValueChangedCallback(evt => CompareValue = evt.newValue);
        mainContainer.Add(valueField);

        RefreshExpandedState();
        RefreshPorts();
    }

    public void SetInitialData(string characterName, string variable, ComparisonType comp, int value)
    {
        CharacterName = characterName;
        SelectedVariable = variable;
        Comparison = comp;
        CompareValue = value;
    }

    public void UpdateUIFromData()
    {
        characterNameField?.SetValueWithoutNotify(CharacterName);
        RefreshVariableDropdown();
        variableDropdown?.SetValueWithoutNotify(SelectedVariable);
        comparisonDropdown?.SetValueWithoutNotify(Comparison.ToString());
        valueField.value = CompareValue;
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

    [System.Serializable]
    private class CharacterIntConditionNodeSerializedData
    {
        public string CharacterName;
        public string SelectedVariable;
        public string Comparison;
        public int CompareValue;
    }

    public override string SerializeNodeData()
    {
        var data = new CharacterIntConditionNodeSerializedData
        {
            CharacterName = CharacterName,
            SelectedVariable = SelectedVariable,
            Comparison = Comparison.ToString(),
            CompareValue = CompareValue
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<CharacterIntConditionNodeSerializedData>(jsonData);
        CharacterName = data.CharacterName;
        SelectedVariable = data.SelectedVariable;
        Comparison = (ComparisonType)Enum.Parse(typeof(ComparisonType), data.Comparison);
        CompareValue = data.CompareValue;

        // Восстановление UI
        RefreshVariableDropdown();
        UpdateUIFromData();
    }
}