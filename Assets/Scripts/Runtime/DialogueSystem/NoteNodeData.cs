using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteNodeData : BaseNodeData
{
    public string NoteText = "";
    public Color BackgroundColor = new Color(1f, 0.9152542f, 0f, 1f);
    public List<string> ConnectedNodeGuids = new List<string>(); // для визуальных связей
}