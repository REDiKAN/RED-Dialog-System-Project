using UnityEngine;

public class MessageConfig : ScriptableObject
{
    [SerializeField] private GameObject _ownerMessagePrefab;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private GameObject _imageMessagePrefab;
    [SerializeField] private GameObject _audioMessagePrefab;
    public GameObject OwnerMessagePrefab => _ownerMessagePrefab;
    public GameObject MessagePrefab => _messagePrefab;
    public GameObject ImageMessagePrefab => _imageMessagePrefab;
    public GameObject AudioMessagePrefab => _audioMessagePrefab;
}
