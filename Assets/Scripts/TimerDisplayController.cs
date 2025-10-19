using UnityEngine;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class TimerDisplayController : MonoBehaviour
{
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Image _fillImage;

    private float _duration;
    private float _elapsed;
    private bool _isRunning;
    private Action _onTimeout;

    private void Update()
    {
        if (!_isRunning || Time.timeScale == 0f) return;

        _elapsed += Time.unscaledDeltaTime;
        float remaining = Mathf.Max(0f, _duration - _elapsed);
        float fill = remaining / _duration;

        _timerText.text = $"{remaining:F2}";
        _fillImage.fillAmount = fill;

        if (remaining <= 0f)
        {
            StopTimer();
            _onTimeout?.Invoke();
        }
    }

    public void StartTimer(float duration, Action onTimeout)
    {
        _duration = duration;
        _elapsed = 0f;
        _onTimeout = onTimeout;
        _isRunning = true;
        _timerText.text = $"{_duration:F2}";
        _fillImage.fillAmount = 1f;
        gameObject.SetActive(true);
    }

    public void StopTimer()
    {
        _isRunning = false;
        gameObject.SetActive(false);
    }
}