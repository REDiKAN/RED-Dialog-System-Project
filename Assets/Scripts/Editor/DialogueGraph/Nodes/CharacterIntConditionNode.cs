using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using DialogueSystem;
using UnityEditor;
using System;
using UnityEditor.Search;

public class CharacterIntConditionNode : BaseConditionNode
{
    public CharacterData CharacterAsset;
    public string SelectedVariable = "";
    public ComparisonType Comparison;
    public int CompareValue;

    private ObjectField characterField;
    private DropdownField variableDropdown;
    private DropdownField comparisonDropdown;
    private IntegerField valueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Character Condition (Int)";

        // Character field (ObjectField גלוסעמ TextField)
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

    public void UpdateUIFromData()
    {
        characterField?.SetValueWithoutNotify(CharacterAsset);
        RefreshVariableDropdown();
        variableDropdown?.SetValueWithoutNotify(SelectedVariable);
        comparisonDropdown?.SetValueWithoutNotify(Comparison.ToString());
        valueField.value = CompareValue;
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

    [System.Serializable]
    private class CharacterIntConditionNodeSerializedData
    {
        public string CharacterAssetGuid;
        public string SelectedVariable;
        public string Comparison;
        public int CompareValue;
    }

    public override string SerializeNodeData()
    {
        string characterGuid = string.Empty;
        if (CharacterAsset != null)
        {
            characterGuid = AssetDatabaseHelper.GetAssetGuid(CharacterAsset);
        }
        var data = new CharacterIntConditionNodeSerializedData
        {
            CharacterAssetGuid = characterGuid,
            SelectedVariable = SelectedVariable,
            Comparison = Comparison.ToString(),
            CompareValue = CompareValue
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<CharacterIntConditionNodeSerializedData>(jsonData);
        // Load character from GUID
        if (!string.IsNullOrEmpty(data.CharacterAssetGuid))
        {
            CharacterAsset = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(data.CharacterAssetGuid);
        }
        SelectedVariable = data.SelectedVariable;
        Comparison = (ComparisonType)Enum.Parse(typeof(ComparisonType), data.Comparison);
        CompareValue = data.CompareValue;
        // Update UI
        RefreshVariableDropdown();
        UpdateUIFromData();
    }
}