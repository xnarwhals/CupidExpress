using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Race Settings")]
    [Tooltip("Number laps in race")]
    [Range(1, 8)]
    public int totalLaps = 3;

    [Tooltip("Max race time")]
    public float maxRaceTime = 300f; // 5 minutes

    [Header("checkpoints")]
    public Transform[] checkpoints;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("Countdown Settings")]
    public float countDownDuration = 3f;
    private float countdownTimer;
    private int lastCountdownNumber = -1; // 

    public enum RaceState
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

    // track carts
    private Dictionary<Cart, CartRaceData> cartRaceData = new Dictionary<Cart, CartRaceData>();
    private List<Cart> finishedCarts = new List<Cart>();

    // Event section
    public Action<RaceState> OnRaceStateChanged;
    public Action<Cart, int> OnCartLapCompleted;
    public Action<Cart, int> OnCartFinished;
    public Action<float> OnRaceTimeUpdate;
    public Action onRaceFinished;

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
    }

    private void Start()
    {
        InitializeRace();
    }

    private void Update()
    {
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
        }

        if (countdownTimer <= 0f)
        {
            SetRaceState(RaceState.Racing);
            raceStartTime = Time.time;
        }
    }

    #region Race Control

    public void StartRace()
    {
        if (currentRaceState != RaceState.WaitingToStart) return;

        SetRaceState(RaceState.CountDown);
        countdownTimer = countDownDuration;
        lastCountdownNumber = -1;

        Debug.Log("Countdown");
    }



    public void PauseRace()
    {
        if (currentRaceState == RaceState.Racing)
        {
            SetRaceState(RaceState.Paused);
            Time.timeScale = 0f; // assuming we used delta time correctly elsewhere
        }
    }

    public void ResumeRace()
    {
        if (currentRaceState == RaceState.Paused)
        {
            SetRaceState(RaceState.Racing);
            Time.timeScale = 1f;
        }
    }

    private void SetRaceState(RaceState newState)
    {
        currentRaceState = newState;
        OnRaceStateChanged?.Invoke(currentRaceState);
        Debug.Log($"Race State: {currentRaceState}");
    }


    #endregion

    #region Cart Management
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

        if (checkpointIndex == data.nextCheckpointIndex) // passed the expected next checkpoint?
        {
            data.nextCheckpointIndex = (checkpointIndex + 1) % checkpoints.Length; // wrap 

            if (checkpointIndex == 0 && data.nextCheckpointIndex == 1 && data.curLap > 1)
            {
                CompleteLap(cart); // lap complete?
            }
            else if (checkpointIndex == 0 && data.firstLoopCheck)
            {
                CompleteLap(cart); 
            }

            if (checkpointIndex == checkpoints.Length - 1)
            {
                data.firstLoopCheck = true; 
            }
        }
    }

    private void CompleteLap(Cart cart)
    {
        var data = cartRaceData[cart];
        data.curLap++;
        data.lapTimes.Add(Time.time - data.raceStartTime); // record lap
        data.lastLapTime = Time.time;

        OnCartLapCompleted?.Invoke(cart, data.curLap);
        // Debug.Log($"{cart.CartName} completed lap {data.curLap - 1}! Total laps: {data.curLap - 1}/{totalLaps}");

        // Check if finished
        if (data.curLap > totalLaps)
        {
            FinishRace(cart);
        }
    }

    private void FinishRace(Cart cart)
    {
        var data = cartRaceData[cart];
        if (data.isFinished) return;

        data.isFinished = true;
        data.finishTime = Time.time - data.raceStartTime;
        finishedCarts.Add(cart);

        int position = finishedCarts.Count; // 1st, 2nd, 3rd, etc.
        OnCartFinished?.Invoke(cart, position);
        Debug.Log($"{cart.CartName} finish in {position}. Time: {data.finishTime:F2} seconds");
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

    public float GetCartRaceTime(Cart cart)
    {
        if (!cartRaceData.ContainsKey(cart)) return 0f;
        return Time.time - cartRaceData[cart].raceStartTime;
    }

    public List<Cart> GetCartLeaderboard()
    {
        return cartRaceData.Keys
            .Where(cart => !cartRaceData[cart].isFinished)
            .OrderByDescending(cart => cartRaceData[cart].curLap)
            .ThenByDescending(cart => cartRaceData[cart].nextCheckpointIndex)
            .ToList();
    }

    public int GetCartPosition(Cart cart)
    {
        if (cartRaceData[cart].isFinished)
        {
            return finishedCarts.IndexOf(cart) + 1;
        }

        var leaderboard = GetCartLeaderboard();
        return leaderboard.IndexOf(cart) + 1;
    }

    #endregion

    #region Utills
    private void InitializeRace()
    {
        currentRaceTime = 0f;
        finishedCarts.Clear();

        // find carts in scene to register
        Cart[] carts = FindObjectsOfType<Cart>();
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
        if (finishedCarts.Count == cartRaceData.Count || (maxRaceTime > 0 && currentRaceTime >= maxRaceTime))
        {
            SetRaceState(RaceState.Finished);
            onRaceFinished?.Invoke();
        }
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
    #endregion

    [Serializable]
    public class CartRaceData
    {
        public Cart cart;
        public int curLap = 1;
        public int nextCheckpointIndex = 0;
        public float raceStartTime;
        public float lastLapTime;
        public float finishTime;
        public bool isFinished = false;
        public bool firstLoopCheck = false;
        public List<float> lapTimes = new List<float>();
    }
}

