using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SynchronizedAction
{
    [Header("Synchronization Settings")]
    [Tooltip("The time interval in seconds for synchronizing the action.")]
    [Range(1f, 5f)]
    public float syncWindow = 3f;

    [Tooltip("Visual feedback during sync attempt")]
    public bool showFeedback = true;

    private HashSet<int> playersPressed = new HashSet<int>();
    private int firstPlayerIndex = -1;
    private bool isActive = false;
    private float timer = 0f;
    private int requiredPlayers = 2;

    // events 
    public event Action<int> OnSyncStarted;
    public event Action OnSyncSuccess;
    public event Action OnSyncFailed;
    public event Action<float> OnSyncProgress; // 0-1

    public void Initialize(int playerCount)
    {
        requiredPlayers = playerCount;
        Reset();
    }

    public bool TryActivate(int playerIndex)
    {
        if (playersPressed.Contains(playerIndex)) return false; // this player already already pressed the button

        playersPressed.Add(playerIndex);

        if (!isActive)
        {
            firstPlayerIndex = playerIndex; // who initiated the sync
            StartSync();
        }

        Debug.Log($"Player {playerIndex + 1} joined sync ({playersPressed.Count}/{requiredPlayers})");
        if (playersPressed.Count >= requiredPlayers)
        {
            CompleteSync();
            return true;
        }
        return false;
    }

    // Start timer and check progress
    public void Update()
    {
        if (!isActive) return;
        timer += Time.deltaTime;
        OnSyncProgress?.Invoke(timer / syncWindow);
        if (timer >= syncWindow)
        {
            FailSync();
        }
    }

    private void StartSync()
    {
        isActive = true;
        timer = 0f;
        OnSyncStarted?.Invoke(firstPlayerIndex);
        Debug.Log("Synchronization started");
    }

    private void CompleteSync()
    {
        isActive = false;
        OnSyncSuccess?.Invoke();
        Debug.Log("Synchronization successful");
        Reset();
    }

    private void FailSync()
    {
        isActive = false;
        OnSyncFailed?.Invoke();
        Debug.Log("Synchronization failed");
        Reset();
    }

    public void Reset()
    {
        playersPressed.Clear();
        isActive = false;
        timer = 0f;
    }
}
