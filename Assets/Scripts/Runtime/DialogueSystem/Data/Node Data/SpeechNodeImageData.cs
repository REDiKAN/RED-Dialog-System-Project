using UnityEngine;
using System;

[Serializable]
public class SpeechNodeImageData : BaseNodeData
{
    public string ImageSpriteGuid = "";
    public string ImageSpritePath = "";

    public string NodeType;
    public string SpeakerGuid;
    public string SpeakerName;
}