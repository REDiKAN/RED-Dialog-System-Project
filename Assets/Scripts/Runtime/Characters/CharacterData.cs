// Assets/Scripts/Runtime/Characters/CharacterData.cs

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Dialogue System/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string FirstName = "New";
    public string LastName = "Character";
    public Sprite Icon;
    [TextArea(3, 10)] public string Description = "";
    public Color NameColor = Color.white;

    [Header("Variables")]
    public List<CharacterVariable> Variables = new List<CharacterVariable>();

    [Header("Message Prefabs")]
    public GameObject SpeechTextMessagePrefab;
    public GameObject SpeechImageMessagePrefab;
    public GameObject SpeechAudioMessagePrefab;

    public void AddVariable(string variableName = "NewVariable", int initialValue = 0)
    {
        Variables.Add(new CharacterVariable(variableName, initialValue));
    }

    public void RemoveVariable(int index)
    {
        if (index >= 0 && index < Variables.Count)
            Variables.RemoveAt(index);
    }

    public bool TryGetVariable(string variableName, out CharacterVariable variable)
    {
        variable = Variables.Find(v => v.VariableName == variableName);
        return variable != null;
    }
}

[System.Serializable]
public class CharacterVariable
{
    public string VariableName = "NewVariable";
    public int Value = 0;

    public CharacterVariable(string name, int value)
    {
        VariableName = name;
        Value = value;
    }
}