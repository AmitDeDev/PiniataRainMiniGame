using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles audio in two categories:
/// 1) background music (looped)
/// 2) one-shot sound effects (SFX)
/// 
/// You must assign in the Inspector:
/// - musicSource: an AudioSource for looping music
/// - sfxSource: an AudioSource for SFX
/// 
/// And supply these AudioClips:
/// - backgroundMusic
/// - piñataClickSounds (List of random click SFX)
/// - bombSound
/// - criticalSound
/// - smashSound (for final piñata destruction)
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource used exclusively for looping background music.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("AudioSource used for one-shot sound effects.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music")]
    [Tooltip("Main looping background music.")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Piñata Click Sounds (Random)")]
    [Tooltip("A list of short SFX for clicking a piñata. One is chosen at random each time.")]
    [SerializeField] private List<AudioClip> piñataClickSounds;

    [Header("Bomb Sound")]
    [Tooltip("SFX for bomb usage.")]
    [SerializeField] private AudioClip bombSound;

    [Header("Critical Sound")]
    [Tooltip("SFX for critical usage.")]
    [SerializeField] private AudioClip criticalSound;

    [Header("Piñata Smash Sound")]
    [Tooltip("SFX for final piñata destruction.")]
    [SerializeField] private AudioClip smashSound;

    private void Start()
    {
        // Loop BGM if assigned
        if (musicSource && backgroundMusic)
        {
            musicSource.loop = true;
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Plays a random piñata click SFX from the list (if not empty).
    /// </summary>
    public void PlayPiniataClickSound()
    {
        if (sfxSource && piñataClickSounds != null && piñataClickSounds.Count > 0)
        {
            int idx = Random.Range(0, piñataClickSounds.Count);
            sfxSource.PlayOneShot(piñataClickSounds[idx]);
        }
    }

    /// <summary>
    /// Plays the bomb SFX once.
    /// </summary>
    public void PlayBombSound()
    {
        if (sfxSource && bombSound)
        {
            sfxSource.PlayOneShot(bombSound);
        }
    }

    /// <summary>
    /// Plays the critical SFX once.
    /// </summary>
    public void PlayCriticalSound()
    {
        if (sfxSource && criticalSound)
        {
            sfxSource.PlayOneShot(criticalSound);
        }
    }

    /// <summary>
    /// Plays the smash SFX (used when a piñata is destroyed).
    /// </summary>
    public void PlayPiniataSmashSound()
    {
        if (sfxSource && smashSound)
        {
            sfxSource.PlayOneShot(smashSound);
        }
    }
}
