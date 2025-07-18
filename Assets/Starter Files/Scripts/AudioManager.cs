using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource sfxSource;
    public float[] sfxPitchSpread;

    void Start()
    {
        // set up singleton
        if (Instance != null) Destroy(gameObject);
        Instance = this;
    }

    void Update()
    {
        // you might put things here for music
    }

    public void PlaySFX(AudioClip sound, float volume, float basePitch = 1)
    {
        // skip null sounds
        if (!sound) return;
        // randomize pitch a bit
        sfxSource.pitch = basePitch + Random.Range(sfxPitchSpread[0], sfxPitchSpread[1]);
        // play sound
        sfxSource.PlayOneShot(sound, volume);
    }
}
