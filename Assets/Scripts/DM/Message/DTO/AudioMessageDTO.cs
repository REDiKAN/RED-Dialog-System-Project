using UnityEngine;

public class AudioMessageDTO : IMessageDTO
{
    public AudioClip Clip { get; private set; }

    public AudioMessageDTO(AudioClip clip)
    {
        Clip = clip;
    }
}
