using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("SFX")]
    public AudioClip shootClip;
    public AudioClip hitClip;
    public AudioClip hitmarkerClip;
    public AudioClip enemyDeathClip;
    public AudioClip playerDeathClip;
    public AudioClip levelUpClip;
    public AudioClip upgradeClip;
    public AudioClip waveStartClip;
    public AudioClip dashClip;

    [Header("Music")]
    public AudioClip gameplayMusic;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicSource != null && gameplayMusic != null)
        {
            musicSource.clip = gameplayMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayShoot()
    {
        PlaySFX(shootClip, 0.3f);
    }

    public void PlayHit()
    {
        PlaySFX(hitClip, 0.5f);
    }

    public void PlayHitmarker()
    {
        PlaySFX(hitmarkerClip, 0.4f);
    }

    public void PlayEnemyDeath()
    {
        PlaySFX(enemyDeathClip, 0.5f);
    }

    public void PlayDeath()
    {
        PlaySFX(playerDeathClip, 1f);
    }

    public void PlayLevelUp()
    {
        PlaySFX(levelUpClip, 0.8f);
    }

    public void PlayUpgrade()
    {
        PlaySFX(upgradeClip, 0.8f);
    }

    public void PlayWaveStart()
    {
        PlaySFX(waveStartClip, 0.7f);
    }

    public void PlayDash()
    {
        PlaySFX(dashClip, 0.5f);
    }

    void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
