using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AudioMessagePresenter : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Slider _slider;
    [SerializeField] private Toggle _toggle;

    private Coroutine _updateCoroutine;
    private bool _initialized;

    public void Initialize(AudioClip audioClip)
    {
        if (audioClip == null || audioClip.length == 0) Debug.LogError("Audio clip length is zero or is null");

        _audioSource.clip = audioClip;
        _slider.value = 0f;

        _toggle.isOn = true;
        _initialized = true;

        TogglePlayer();
    }

    public void OnSliderInterract()
    {
        if (!_initialized) return;

        bool wasPlaying = _toggle.isOn;

        _audioSource.time = _audioSource.clip.length * _slider.value;

        if (wasPlaying && !_audioSource.isPlaying)
        {
            _audioSource.Play();
        }
    }

    public void TogglePlayer()
    {
        if (!_initialized) return;

        if (_toggle.isOn)
        {
            if (!_audioSource.isPlaying)
                _audioSource.Play();

            if (_updateCoroutine == null)
                _updateCoroutine = StartCoroutine(UpdateSlider());
        }
        else
        {
            _audioSource.Pause();
            StopUpdateRoutine();
        }
    }

    private IEnumerator UpdateSlider()
    {
        while (_audioSource != null && _audioSource.isPlaying)
        {
            if (_audioSource.clip != null && _audioSource.clip.length > 0f)
                _slider.value = _audioSource.time / _audioSource.clip.length;

            yield return null;
        }

        OnAfterMessagePlayed();
    }

    private void OnAfterMessagePlayed()
    {
        StopUpdateRoutine();
        _toggle.isOn = false;
        _slider.value = 0f;
        _audioSource.time = 0f;
    }

    private void OnDisable()
    {
        StopUpdateRoutine();
    }

    private void StopUpdateRoutine()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }
    }
}
