using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Clips")]
    public AudioClip engineLoopClip;
    public AudioClip laneSwitchClip;
    public AudioClip hitClip;
    public AudioClip bgmClip;
    public AudioClip coinClip;

    [Header("Levels")]
    [Range(0f, 1f)]
    public float engineVolume = 0.45f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;
    [Range(0f, 1f)]
    public float bgmVolume = 0.35f;
    public bool playBgmOnAwake = true;

    private AudioSource engineSource;
    private AudioSource sfxSource;
    private AudioSource bgmSource;

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
        if (!bgmClip)
        {
            bgmClip = Resources.Load<AudioClip>("Audio/SpaceBeat2");
        }
        if (!coinClip)
        {
            coinClip = Resources.Load<AudioClip>("Audio/Coin");
        }
        if (!hitClip)
        {
            hitClip = Resources.Load<AudioClip>("Audio/Hit");
        }

        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.volume = engineVolume;
        engineSource.clip = engineLoopClip;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;
        bgmSource.clip = bgmClip;

        if (playBgmOnAwake && bgmClip)
        {
            bgmSource.Play();
        }
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

    public void PlayCoin()
    {
        if (coinClip && sfxSource)
        {
            sfxSource.PlayOneShot(coinClip, sfxVolume * 0.7f);
        }
    }

    public void SetBgmEnabled(bool enabled)
    {
        if (!bgmSource) return;
        if (enabled)
        {
            if (!bgmSource.isPlaying) bgmSource.Play();
        }
        else
        {
            if (bgmSource.isPlaying) bgmSource.Pause();
        }
    }
}
