using UnityEngine;

public class ImageMessageDTO : IMessageDTO
{
    public Sprite Image { get; private set; }

    public ImageMessageDTO(Sprite image) 
    { 
        Image = image;
    }
}
