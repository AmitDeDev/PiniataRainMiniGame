using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music")]
    [Tooltip("Main looping background music")]
    [SerializeField] private AudioClip backgroundMusic;

    [FormerlySerializedAs("piñataClickSounds")]
    [Header("Piniata Click Sounds (Random)")]
    [Tooltip("A list of short SFX for clicking a Piniata. One is chosen at random each time.")]
    [SerializeField] private List<AudioClip> piniataClickSounds;

    [Header("Bomb Sound")]
    [Tooltip("SFX for bomb usage.")]
    [SerializeField] private AudioClip bombSound;

    [Header("Critical Sound")]
    [Tooltip("SFX for critical usage.")]
    [SerializeField] private AudioClip criticalSound;

    [Header("Piniata Smash Sound")]
    [Tooltip("SFX for final piñata destruction.")]
    [SerializeField] private AudioClip smashSound;

    private void Start()
    {
        if (musicSource && backgroundMusic)
        {
            musicSource.loop = true;
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }
    
    public void PlayPiniataClickSound()
    {
        if (sfxSource && piniataClickSounds != null && piniataClickSounds.Count > 0)
        {
            int idx = Random.Range(0, piniataClickSounds.Count);
            sfxSource.PlayOneShot(piniataClickSounds[idx]);
        }
    }
    
    public void PlayBombSound()
    {
        if (sfxSource && bombSound)
        {
            sfxSource.PlayOneShot(bombSound);
        }
    }

    public void PlayCriticalSound()
    {
        if (sfxSource && criticalSound)
        {
            sfxSource.PlayOneShot(criticalSound);
        }
    }

    public void PlayPiniataSmashSound()
    {
        if (sfxSource && smashSound)
        {
            sfxSource.PlayOneShot(smashSound);
        }
    }
}
