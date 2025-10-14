using System;
using System.Collections.Generic;

[Serializable]
public class SpeechRandNodeData : BaseNodeData
{
    public string SpeakerName;
    public List<SpeechVariant> Variants = new List<SpeechVariant>();
}

[Serializable]
public class SpeechVariant
{
    public string Text = "";
    public float WeightPercent = 0f; // 0Ц100, сумма всех = 100
}
