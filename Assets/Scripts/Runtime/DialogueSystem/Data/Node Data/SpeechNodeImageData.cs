using UnityEngine;
using System;

[Serializable]
public class SpeechNodeImageData : BaseNodeData
{
    public string ImageSpritePath = "";

    public string NodeType;
    public string SpeakerGuid;
    public string SpeakerName;
}