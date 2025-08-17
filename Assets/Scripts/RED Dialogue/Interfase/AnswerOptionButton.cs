using UnityEngine;
using TMPro;

public class AnswerOptionButton : MonoBehaviour
{
    [SerializeField] private TMP_Text answerOptionText;

    public void SetAnswerOptionText(string text)
    {
        answerOptionText.text = text;
    }
}
