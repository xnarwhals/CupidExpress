using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;

    [Header("Race Music")]
    public AudioClip menuMusic;
    public AudioClip raceMusic;
    public AudioClip victoryMusic; // optional
    public AudioClip defeatMusic; // optional

    [Header("SFX Clips")]
    public AudioClip countDownBeep;
    public AudioClip raceStartSound;
    public AudioClip lapCompleteSound;

    [Header("Item SFX")]
    public AudioClip itemPickupSound;
    public AudioClip tomatoThrowSound; // optional
    public AudioClip tomatoHitSound;
    public AudioClip sodaBlastSound;

    [Header("UI Sounds")]
    public AudioClip pauseSound;
    public AudioClip unpauseSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;
    [Range(0f, 1f)] public float uiVolume = 0.5f;

    // pooling here? maybe idk

    private AudioClip currentMusic;
    private bool isMusicPlaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();

        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    private void Start()
    {
        SubscribeToRaceStart();
    }

    private void InitializeAudioManager()
    {
        ApplySoundSettings();

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
        }

        if (uiSource != null)
        {
            uiSource.playOnAwake = false;
        }
    }

    private void ApplySoundSettings()
    {

    }

    #region Subscription Methods

    public void SubscribeToRaceStart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRaceStateChanged += HandleRaceStateChanged;
            GameManager.Instance.OnCartLapCompleted += HandleCartLapCompleted;
            // GameManager.Instance.OnCartFinished += HandleCartFinished;
            GameManager.Instance.OnCountdownUpdate += HandleRaceTimeUpdate; // note
            GameManager.Instance.CountdownGO += HandleCountdownGO;
            GameManager.Instance.OnRaceFinished += HandleRaceFinished;
        }

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemPickup += HandleItemPickup;
        }

    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRaceStateChanged -= HandleRaceStateChanged;
            GameManager.Instance.OnCartLapCompleted -= HandleCartLapCompleted;
            // GameManager.Instance.OnCartFinished -= HandleCartFinished;
            GameManager.Instance.OnCountdownUpdate -= HandleRaceTimeUpdate;
            GameManager.Instance.CountdownGO -= HandleCountdownGO;
            GameManager.Instance.OnRaceFinished -= HandleRaceFinished;
        }

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemPickup -= HandleItemPickup;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleRaceStateChanged(GameManager.RaceState newState)
    {
        switch (newState)
        {
            // Menu
            case GameManager.RaceState.WaitingToStart:
                PlayMusic(menuMusic);
                break;
            case GameManager.RaceState.CountDown:
                // PlaySFX(countDownBeep);
                break;
            case GameManager.RaceState.Racing:
                PlayMusic(raceMusic);
                break;
            case GameManager.RaceState.Paused:
                PauseMusic();
                PlayUISFX(pauseSound);
                break;
            case GameManager.RaceState.Finished:
                // win/lose music 
                break;
        }
    }

    private void HandleRaceTimeUpdate(int countdownNumber)
    { // called elsewhere
        PlaySFX(countDownBeep, 0.8f);
    }
    
    // race start GO
    private void HandleCountdownGO()
    { // should be called once per game 
        PlaySFX(raceStartSound, 1f);
        PlayMusic(raceMusic);
    }

    private void HandleCartLapCompleted(Cart cart, int lapNumber)
    {
        if (cart.CartID == 0)
        {
            PlaySFX(lapCompleteSound, 0.7f);
        }
        {
            PlaySFX(lapCompleteSound, 0.7f);
        }
    }

    // private void HandleCartFinished(Cart cart)
    // {
    //     int cartPosition = GameManager.Instance.GetCartPosition(cart);

    //     if (cart.CartID == 0 && cartPosition >= 1 && cartPosition <= 3) // player cart and in top 3
    //     {
    //         PlaySFX(victoryMusic, 1f);
    //     }
    //     else
    //     {
    //         PlaySFX(defeatMusic, 1f);
    //     }
    // }

    private void HandleRaceFinished()
    {
        if (GameManager.Instance.PlayerCartWon())
        {
            PlaySFX(victoryMusic, 1f);
        }
        else
        {
            PlaySFX(defeatMusic, 1f);
        }
    }

    private void HandleItemPickup(Cart cart)
    {
        if (cart.CartID == 0)
        {
            PlaySFX(itemPickupSound, 0.5f);
        }
    }

    #endregion

    #region Music Control

    // Add fade in/out later
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || clip == currentMusic) return;

        if (musicSource != null)
        {
            musicSource.clip = clip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
            currentMusic = clip;
            isMusicPlaying = true;
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            isMusicPlaying = false;
            currentMusic = null;
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null && isMusicPlaying)
        {
            musicSource.Pause();
            isMusicPlaying = false;
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null && !isMusicPlaying)
        {
            musicSource.UnPause();
            isMusicPlaying = true;
        }
    }

    #endregion

    #region SFX Control
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        float sfxVolume = Mathf.Clamp01(volume) * masterVolume;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayUISFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || uiSource == null) return;
        float uiVolume = Mathf.Clamp01(volume) * masterVolume;
        uiSource.PlayOneShot(clip, uiVolume);
    }


    #endregion

    // item methods
    public void PlayTomatoHit()
    {
        PlaySFX(tomatoHitSound, 0.5f);
    }

    public void PlayTomatoThrow()
    {
        PlaySFX(tomatoThrowSound, 0.5f);
    }

    public void PlaySodaBlast()
    {
        PlaySFX(sodaBlastSound, 0.5f);
    }


    #region Volume Control
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        if (uiSource != null)
        {
            uiSource.volume = uiVolume * masterVolume;
        }
    }

    private void ApplyVolumeSettings()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    #endregion

    #region ultills
    public bool IsMusicPlaying()
    {
        return isMusicPlaying;
    }
    
    public AudioClip GetCurrentMusic()
    {
        return currentMusic;
    }

    #endregion







}
