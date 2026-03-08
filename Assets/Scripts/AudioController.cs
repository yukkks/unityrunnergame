using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Clips")]
    public AudioClip engineLoopClip;
    public AudioClip laneSwitchClip;
    public AudioClip hitClip;

    [Header("Levels")]
    [Range(0f, 1f)]
    public float engineVolume = 0.45f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;

    private AudioSource engineSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SetupSources();
    }

    void SetupSources()
    {
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.volume = engineVolume;
        engineSource.clip = engineLoopClip;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    public void SetRunning(bool running)
    {
        if (!engineSource) return;
        if (!engineLoopClip) return;

        if (running)
        {
            if (!engineSource.isPlaying) engineSource.Play();
        }
        else
        {
            if (engineSource.isPlaying) engineSource.Pause();
        }
    }

    public void PlayLaneSwitch()
    {
        if (laneSwitchClip && sfxSource)
        {
            sfxSource.PlayOneShot(laneSwitchClip, sfxVolume);
        }
    }

    public void PlayHit()
    {
        if (hitClip && sfxSource)
        {
            sfxSource.PlayOneShot(hitClip, sfxVolume);
        }
    }
}
