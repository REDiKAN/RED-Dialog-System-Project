using UnityEngine;
using UnityEngine.UI;

public class ImageMessagePresenter : MonoBehaviour
{
    [SerializeField] private Image _image;

    public void Initialize(Sprite image)
    {
        _image.sprite = image;
    }
}
