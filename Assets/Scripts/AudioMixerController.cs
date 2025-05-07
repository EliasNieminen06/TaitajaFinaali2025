using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer
using UnityEngine.UI;   // Required for Slider

/// <summary>
/// Controls Audio Mixer group volumes using UI Sliders.
/// </summary>
public class AudioMixerController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [Tooltip("The Audio Mixer asset to control.")]
    public AudioMixer mainAudioMixer;

    [Header("Exposed Parameters")]
    [Tooltip("The name of the exposed volume parameter for the Music group.")]
    public string musicVolumeParameter = "MusicVolume"; // Default name, change if yours is different
    [Tooltip("The name of the exposed volume parameter for the SFX group.")]
    public string sfxVolumeParameter = "SFXVolume";     // Default name, change if yours is different

    [Header("UI Sliders")]
    [Tooltip("The UI Slider that controls the Music volume.")]
    public Slider musicVolumeSlider;
    [Tooltip("The UI Slider that controls the SFX volume.")]
    public Slider sfxVolumeSlider;

    private void Awake()
    {
        // Optional: Set initial slider values based on current mixer volume
        // This requires getting the current volume, which can be a bit tricky
        // as GetFloat returns the value in dB. You might need conversion.
        // For simplicity, we'll just add listeners here.
    }

    private void OnEnable()
    {
        // Add listeners to the sliders' value change events
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }
    }

    private void OnDisable()
    {
        // Remove listeners when the object is disabled to prevent memory leaks
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
        }
    }

    /// <summary>
    /// Sets the Music volume based on the slider value.
    /// Converts the linear slider value (0-1) to decibels (dB).
    /// </summary>
    /// <param name="volume">The volume value from the slider (0 to 1).</param>
    public void SetMusicVolume(float volume)
    {
        // Audio Mixer volumes are typically controlled in decibels (dB).
        // A common conversion is using Mathf.Log10.
        // Volume 0 should map to -80 dB (or lower, essentially silent).
        // Volume 1 should map to 0 dB (full volume).
        // We use Mathf.Log10(volume) * 20 for a logarithmic scale.
        // We add 0.0001f to volume to avoid Log10(0) which is negative infinity.
        float dbVolume = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        mainAudioMixer.SetFloat(musicVolumeParameter, dbVolume);
    }

    /// <summary>
    /// Sets the SFX volume based on the slider value.
    /// Converts the linear slider value (0-1) to decibels (dB).
    /// </summary>
    /// <param name="volume">The volume value from the slider (0 to 1).</param>
    public void SetSfxVolume(float volume)
    {
        // Same conversion logic as for music volume.
        float dbVolume = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        mainAudioMixer.SetFloat(sfxVolumeParameter, dbVolume);
    }

    // You could add more methods here for other volume groups (e.g., Master)
    // public void SetMasterVolume(float volume) { ... }
}
