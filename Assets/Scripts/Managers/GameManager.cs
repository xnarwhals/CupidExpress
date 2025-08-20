using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Splines;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Race Settings")]
    [Tooltip("Number laps in race")]
    [Range(1, 8)]
    public int totalLaps = 3;

    [Tooltip("Max race time")]
    public float maxRaceTime = 300f; // 5 minutes

    [Header("checkpoints/race track")]
    public Transform checkpointHolder;
    public Transform[] checkpoints;
    public SplineContainer raceTrack;


    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("Countdown Settings")] // we hate coroutines
    public float countDownDuration = 3f;
    private float countdownTimer;
    private int lastCountdownNumber = -1; 
    public float startCountdownDelay = 4f; // delay before countdown starts
    private float startCountdownTimer;

    public enum RaceState // add more if needed later
    {
        WaitingToStart,
        CountDown,
        Racing,
        Finished,
        Paused
    }

    [SerializeField] private RaceState currentRaceState = RaceState.WaitingToStart;
    private float raceStartTime;
    private float currentRaceTime;

    // Track all carts
    private Dictionary<Cart, CartRaceData> cartRaceData = new Dictionary<Cart, CartRaceData>(); // Each cart has it's race data
    private List<Cart> finishedCarts = new List<Cart>();
    private Cart[] allCarts;
    public Cart[] AllCarts => allCarts;

    // Event section
    public Action<RaceState> OnRaceStateChanged;
    public Action<Cart, int> OnCartLapCompleted;
    public Action<Cart, int> OnCartFinished;
    public Action<float> OnRaceTimeUpdate;
    public Action OnRaceFinished;
    public Action<int> OnCountdownUpdate; // For UI 
    public Action<Cart, int> OnCartPositionChanged;
    public Action CountdownGO;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeRace();
    }

    private void Start()
    {
        // PrintLeaderboardPositions();
        allCarts = FindObjectsOfType<Cart>()
            .OrderByDescending(cart => cart.CartID) // sort by ID
            .ToArray();
    }

    private void Update()
    {
        if (currentRaceState == RaceState.WaitingToStart)
        {
            startCountdownTimer += Time.deltaTime;
            if (startCountdownTimer >= startCountdownDelay)
            {
                StartRace();
            }
        }
        

        if (currentRaceState == RaceState.CountDown)
        {
            UpdateCountdown();
        }

        if (currentRaceState == RaceState.Racing)
        {
            UpdateRaceTime();
            CheckForRaceCompletion();
        }


        if (debugMode)
        {
            DebugRaceInfo();
        }
    }


    private void UpdateCountdown()
    {
        countdownTimer -= Time.deltaTime;
        int currentNum = Mathf.CeilToInt(countdownTimer); // 3 2 1

        if (currentNum != lastCountdownNumber && currentNum > 0)
        {
            lastCountdownNumber = currentNum;
            OnCountdownUpdate?.Invoke(currentNum);

            // AudioManager.Instance.PlayUISFX(AudioManager.Instance.countDownBeep, 1.0f);
        }

        if (countdownTimer <= 0f)
        {
            CountdownGO?.Invoke();
            SetRaceState(RaceState.Racing);
            raceStartTime = Time.time;

            // AudioManager.Instance.PlayUISFX(AudioManager.Instance.raceStartSound, 1.0f);
        }
    }

    #region Race Control
    public void StartRace()
    {
        if (currentRaceState != RaceState.WaitingToStart) return; // state progression WaitingToStart -> CountDown 

        SetRaceState(RaceState.CountDown);
        countdownTimer = countDownDuration;
        lastCountdownNumber = -1;
    }

    public void PauseRace()
    {
        if (currentRaceState == RaceState.Racing)
        {
            SetRaceState(RaceState.Paused);
            Time.timeScale = 0f; // assuming we used delta time correctly elsewhere

            AudioManager.Instance.PlayUISFX(AudioManager.Instance.pauseSound, 1.0f);
        }
    }

    public void ResumeRace()
    {
        if (currentRaceState == RaceState.Paused)
        {
            SetRaceState(RaceState.Racing);
            Time.timeScale = 1f;

            AudioManager.Instance.PlayUISFX(AudioManager.Instance.unpauseSound, 1.0f);
        }
    }


    private void SetRaceState(RaceState newState)
    {
        currentRaceState = newState;
        OnRaceStateChanged?.Invoke(currentRaceState);
        // Debug.Log($"Race State: {currentRaceState}");
    }

    public void RestartRace()
    {
        Time.timeScale = 1f; // Ensure time is running
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); 
        InitializeRace();
        // SceneLoader.Instance.LoadScene(0);

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAllAudio();

        SceneManager.LoadScene(0);
    }


    #endregion

    #region Cart Management
    // Register player and AI carts with their data
    private void RegisterCart(Cart cart)
    {
        if (!cartRaceData.ContainsKey(cart))
        {
            cartRaceData[cart] = new CartRaceData
            {
                cart = cart,
                curLap = 1,
                nextCheckpointIndex = 0,
                raceStartTime = Time.time,
                isFinished = false
            };

        }
    }
    public void OnCartPassedCheckpoint(Cart cart, int checkpointIndex)
    {
        if (!cartRaceData.ContainsKey(cart) || currentRaceState != RaceState.Racing) return;
        var data = cartRaceData[cart];

        if (checkpointIndex == data.nextCheckpointIndex)
        {
            data.lastCheckpointPassed = checkpointIndex;
            // If last checkpoint, set flag
            if (checkpointIndex == checkpoints.Length - 1)
            {
                data.hasPassedLastCheckpoint = true;
            }

            // If start checkpoint (0) and has passed last checkpoint, complete lap
            if (checkpointIndex == 0 && data.hasPassedLastCheckpoint)
            {
                CompleteLap(cart);
                data.hasPassedLastCheckpoint = false; // Reset for next lap
            }

            data.nextCheckpointIndex = (checkpointIndex + 1) % checkpoints.Length;
        }
        NotifyCartPositions();
    }

    private void CompleteLap(Cart cart)
    {
        var data = cartRaceData[cart];
        data.curLap++;
        data.lapTimes.Add(Time.time - data.raceStartTime); // record lap
        data.lastLapTime = Time.time;

        OnCartLapCompleted?.Invoke(cart, data.curLap);
        Debug.Log($"{cart.CartName} completed lap {data.curLap - 1}! Total laps: {data.curLap - 1}/{totalLaps}");

        // Check if finished + leaderboard stuff later
        if (data.curLap > totalLaps)
        {
            FinishRace(cart);
        }
        
        NotifyCartPositions();
    }

    private void FinishRace(Cart cart)
    {
        var data = cartRaceData[cart];

        if (data.isFinished) return; // player or bot already won 

        data.isFinished = true;
        data.finishTime = Time.time - data.raceStartTime;
        finishedCarts.Add(cart); // use for leaderboard

        int position = finishedCarts.Count; // 1st, 2nd, 3rd, etc.
        OnCartFinished?.Invoke(cart, position);
        Debug.Log($"{cart.CartName} finish in {position}. Time: {data.finishTime:F2} seconds");
    }
    
    private void NotifyCartPositions()
    {
        var leaderboard = GetCartLeaderboard();
        for (int i = 0; i < leaderboard.Count; i++)
        {
            OnCartPositionChanged?.Invoke(leaderboard[i], i + 1); // 1-based position
        }
    }

    #endregion

    #region Race Info

    public RaceState GetCurrentRaceState()
    {
        return currentRaceState;
    }
    public int GetCartLap(Cart cart)
    {
        return cartRaceData.ContainsKey(cart) ? cartRaceData[cart].curLap : 0;
    }

    public int GetCartCheckpoint(Cart cart)
    {
        return cartRaceData.ContainsKey(cart) ? cartRaceData[cart].nextCheckpointIndex : 0;
    }

    public bool PlayerCartWon()
    {
        Cart finishedCarts = GetCartLeaderboard().FirstOrDefault();
        return finishedCarts != null && finishedCarts.CartID == 0; // player

    }

    public float GetCartRaceTime(Cart cart)
    {
        if (!cartRaceData.ContainsKey(cart)) return 0f;
        var data = cartRaceData[cart];

        if (data.isFinished)
        {
            return data.finishTime;
        }

        return Time.time - data.raceStartTime;
    }

    public List<Cart> GetCartLeaderboard()
    {
        // first filter finished carts, then order by lap and checkpoint
        var unfinished = cartRaceData.Keys
            .Where(cart => !cartRaceData[cart].isFinished)
            .OrderByDescending(cart => GetTrackProgress(cart)) // sort by progress
            .ToList();

        var full = new List<Cart>(finishedCarts);
        full.AddRange(unfinished);
        return full;
    }

    public float GetTrackProgress(Cart cart)
    {
        var data = cartRaceData[cart];
        int lap = data.curLap;
        int checkpoint = data.lastCheckpointPassed;
        float splineProgress = cart.GetSplineProgress(); // normalized 0-1 between checkpoints

        // Combine all into a single value
        return lap * checkpoints.Length + checkpoint + splineProgress;
    }

    public Cart GetLeaderCart()
    {
        var leaderboard = GetCartLeaderboard();
        return leaderboard.Count > 0 ? leaderboard[0] : null; // first place
    }

    public int GetCartPosition(Cart cart)
    {
        if (!cartRaceData.ContainsKey(cart))
        {
            Debug.LogWarning($"Cart {cart.CartName} not found");
            return -1; // not registered
        }

        if (cartRaceData[cart].isFinished)
        {
            return finishedCarts.IndexOf(cart) + 1;
        }

        // use leaderboard if not finished
        var leaderboard = GetCartLeaderboard();
        return leaderboard.IndexOf(cart) + 1;
    }

    //testing
    public void SetCartLap(Cart cart, int lap)
    {
        if (cartRaceData.ContainsKey(cart))
        {
            cartRaceData[cart].curLap = lap;
            cartRaceData[cart].nextCheckpointIndex = 0;
        }
    }

    #endregion

    #region Utills
    private void InitializeRace()
    {
        currentRaceTime = 0f;
        finishedCarts.Clear();

        // checkpoint assignment
        if (checkpointHolder != null)
        {
            checkpoints = checkpointHolder.GetComponentsInChildren<Transform>()
                .Where(t => t != checkpointHolder.transform) // exclude the holder itself
                .ToArray();
        }
        else
        {
            Debug.LogWarning("Checkpoint holder not assigned! Please assign it in the GameManager.");
            checkpoints = new Transform[0];
        }


        // find carts in scene to register
        Cart[] carts = FindObjectsOfType<Cart>()
            .OrderBy(cart => cart.CartID) // sort by ID
            .ToArray();

        foreach (Cart cart in carts)
        {
            RegisterCart(cart);
        }
        Debug.Log($"Race initialized with {carts.Length} carts and {totalLaps} laps");

    }

    private void UpdateRaceTime()
    {
        currentRaceTime = Time.time - raceStartTime;
        OnRaceTimeUpdate?.Invoke(currentRaceTime);

        if (maxRaceTime > 0 && currentRaceTime >= maxRaceTime) SetRaceState(RaceState.Finished); 
    }

    private void CheckForRaceCompletion()
    {
        bool playerCartFinished = finishedCarts.Any(cart => cart.CartID == 0);

        // (maxRaceTime > 0 && currentRaceTime >= maxRaceTime)
        if (playerCartFinished)
        {
            var playerCart = finishedCarts.First(cart => cart.CartID == 0);
            float playerFinishTime = cartRaceData[playerCart].finishTime;
            LocalLeaderboard.AddTime(playerFinishTime, playerCart.CartName);

            SetRaceState(RaceState.Finished);
            OnRaceFinished?.Invoke();
            // SceneLoader.Instance.LoadScene(0); 
        }
    }

    public bool AICanMoveState()
    {
        return currentRaceState == RaceState.Racing || currentRaceState == RaceState.Finished;
    }
    
    private void DebugRaceInfo()
    {
        if (currentRaceState == RaceState.Racing && cartRaceData.Count > 0)
        {
            var leaderboard = GetCartLeaderboard();
            if (leaderboard.Count > 0)
            {
                var curFirstPlace = leaderboard[0];
                var curFirstPlaceData = cartRaceData[curFirstPlace];
                Debug.Log($"Leader: {curFirstPlace.CartName} - Lap {curFirstPlaceData.curLap}/{totalLaps}");
            }
        }
    }

    private void PrintLeaderboardPositions()
    {
        var leaderboard = GetCartLeaderboard();
        for (int i = 0; i < leaderboard.Count; i++)
        {
            var cart = leaderboard[i];
            Debug.Log($"{i + 1}: {cart.CartName} (Lap {cartRaceData[cart].curLap}/{totalLaps}, Checkpoint {cartRaceData[cart].nextCheckpointIndex + 1}/{checkpoints.Length})");
        }
    }

    public float GetCheckpointProgress(Cart cart)
    {
        var data = cartRaceData[cart];
        // If nextCheckpointIndex is 0, treat it as being just after the last checkpoint
        return (data.nextCheckpointIndex == 0 ? checkpoints.Length : data.nextCheckpointIndex);
    }  

    public Cart GetPlayerCart()
    {
        return cartRaceData.Keys.FirstOrDefault(cart => cart.CartID == 0); 
    }         

    #endregion

    [Serializable]
    public class CartRaceData
    {
        public Cart cart;
        public int curLap = 1;
        public int nextCheckpointIndex = 0;
        public int lastCheckpointPassed = -1;
        public float raceStartTime;
        public float lastLapTime;
        public float finishTime;
        public bool isFinished = false;
        public bool hasPassedLastCheckpoint = false;
        public List<float> lapTimes = new List<float>();
    }
}

