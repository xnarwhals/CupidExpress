using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private bool gameManagerSubscribed = false;
    public static AudioManager Instance { get; private set; }
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;
    public AudioSource eternalAmbience;

    [Header("Race Music")]
    public AudioClip menuMusic;
    public AudioClip raceMusic;
    public AudioClip lastLapMusic;
    public AudioClip victoryMusic; // optional
    public AudioClip defeatMusic; // optional

    [Header("SFX Clips")]
    public AudioClip countDownBeep;
    public AudioClip raceStartSound;
    public AudioClip lapCompleteSound;
    public AudioClip hitConeSound;
    public AudioClip environmental;
    public AudioClip portal; 

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
    private bool isOnLastLap = false;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            PlayMusic(menuMusic);
        }
        else
        PlayConstantAmbience();
    }

    public void StopAllAudio()
    {
        StopMusic();
        if (sfxSource != null) sfxSource.Stop();   // stops any PlayOneShot currently playing
        if (uiSource  != null) uiSource.Stop();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameManagerSubscribed = false; 
        StopAllAudio();
        if (scene.buildIndex == 0) // Main Menu
        {
            isOnLastLap = false;        // reset any race state flags
            PlayMusic(menuMusic);       // explicitly start menu music
        }
        else if (scene.buildIndex == 1 || scene.buildIndex == 2)
        {
            
        }
        else PlayConstantAmbience();

        TrySubscribeManagers();
    }

    private void TrySubscribeManagers()
    {
        if (!gameManagerSubscribed && GameManager.Instance != null)
        {
            gameManagerSubscribed = true;
            SubscribeToRaceStart();
        }
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
            GameManager.Instance.OnCartFinished += HandleCartFinished;
            // GameManager.Instance.OnCountdownUpdate += HandleRaceTimeUpdate; // note
            GameManager.Instance.CountdownGO += HandleCountdownGO;
            // GameManager.Instance.OnRaceFinished += HandleRaceFinished;
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
            GameManager.Instance.OnCartFinished -= HandleCartFinished;
            // GameManager.Instance.OnCountdownUpdate -= HandleRaceTimeUpdate;
            GameManager.Instance.CountdownGO -= HandleCountdownGO;
            // GameManager.Instance.OnRaceFinished -= HandleRaceFinished;
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
                isOnLastLap = false;
                break;
            case GameManager.RaceState.CountDown:
                PlaySFX(countDownBeep);
                break;
            case GameManager.RaceState.Racing:
                if (isOnLastLap) 
                    PlayMusic(lastLapMusic);
                else
                    PlayMusic(raceMusic);
                break;
            case GameManager.RaceState.Paused:
                break;
            case GameManager.RaceState.Finished:
                PauseMusic();

                // win/lose music 
                break;
        }
    }

    // private void HandleRaceTimeUpdate(int countdownNumber)
    // { // called elsewhere
    //     PlaySFX(countDownBeep, 0.8f);
    // }
    
    // race start GO
    private void HandleCountdownGO()
    { // should be called once per game 
        // PlaySFX(raceStartSound, 1f);
        PlayMusic(raceMusic);
    }

    private void HandleCartLapCompleted(Cart cart, int lapNumber)
    {
        if (cart.CartID == 0)
        {
            PlaySFX(lapCompleteSound, 0.7f);
            if (lapNumber == GameManager.Instance.totalLaps)
            {
                Debug.Log("Last lap reached");
                isOnLastLap = true;
                PlayMusic(lastLapMusic);
            }
        }
    }

    private void HandleCartFinished(Cart cart, int pos)
    {
        if (cart.CartID == 0 && pos >= 1 && pos <= 3) // player cart and in top 3
        {
            PlaySFX(victoryMusic, 1f);
        }
        else
        {
            PlaySFX(defeatMusic, 1f);
        }
    }

    // private void HandleRaceFinished()
    // {
    //     if (GameManager.Instance.PlayerCartWon())
    //     {
    //         PlaySFX(victoryMusic, 1f);
    //     }
    //     else
    //     {
    //         PlaySFX(defeatMusic, 1f);
    //     }
    // }

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

    #region Item SFX
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

    #endregion

    // non item sfx methdods
    public void PlayHitCone()
    {
        PlaySFX(hitConeSound, 0.5f);
    }

    public void PlayPortalSound()
    {
        PlaySFX(portal, 0.5f);
    }

    public void PlayConstantAmbience()
    {
        eternalAmbience.clip = environmental;
        eternalAmbience.Play();
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
