// File: Assets/Scripts/Runtime/DialogueSystem/Data/Node Data/CharacterButtonPressNodeData.cs
using System;
using UnityEngine;

[Serializable]
public class CharacterButtonPressNodeData : BaseNodeData
{
    public string CharacterName;
    public bool RequireButtonPress;
}