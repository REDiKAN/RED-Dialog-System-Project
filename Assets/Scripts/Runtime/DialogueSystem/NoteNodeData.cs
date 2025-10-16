using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteNodeData : BaseNodeData
{
    public string NoteText = "";
    public Color BackgroundColor = new Color(1f, 0.98f, 0.77f, 1f); // Желтый по умолчанию
    public List<string> ConnectedNodeGuids = new List<string>(); // для визуальных связей
}