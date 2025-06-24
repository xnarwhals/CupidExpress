using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem.Composites;

public class CartRoleManager : MonoBehaviour
{
    public static CartRoleManager Instance { get; private set; }
    private List<CartPlayerInput> joinedPlayers = new List<CartPlayerInput>();

    [Header("Role Swap Settings")]
    [SerializeField] private SynchronizedAction roleSwapSync = new SynchronizedAction();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Initialize sync events
        roleSwapSync.OnSyncStarted += OnRoleSwapStarted;
        roleSwapSync.OnSyncSuccess += OnRoleSwapSuccess;
        roleSwapSync.OnSyncFailed += OnRoleSwapFailed;
    }

    private void Update()
    {
        roleSwapSync.Update();
    }

    public void RegisterPlayer(CartPlayerInput player)
    {
        joinedPlayers.Add(player);
        if (joinedPlayers.Count == 1)
        {
            player.AssignRole(CartRole.Driver);
        }
        else if (joinedPlayers.Count == 2)
        {
            player.AssignRole(CartRole.Passenger);
            roleSwapSync.Initialize(joinedPlayers.Count);
        }
        else
        {
            Debug.LogWarning("More than two players joined");
        }
    }

    public void TrySwapRoles(int playerIndex)
    {
        if (joinedPlayers.Count < 2) return;
        roleSwapSync.TryActivate(playerIndex);
    }

    private void OnRoleSwapStarted()
    {
        Debug.Log("Role swap started"); // one player pressed Y
        if (roleSwapSync.showFeedback)
        {
            // change UI color, play sound, etc.
        }
    }

    private void OnRoleSwapSuccess()
    {
        Debug.Log("Sync swap success!");

        var p1 = joinedPlayers[0];
        var p2 = joinedPlayers[1];
        var r1 = p1.role;
        var r2 = p2.role;

        p1.AssignRole(r2);
        p2.AssignRole(r1);

        // apply mini boost here?
        Debug.Log("Roles swapped: " + joinedPlayers[0].role + " <-> " + joinedPlayers[1].role);
    }

    private void OnRoleSwapFailed()
    {
        Debug.Log("Role swap sync failed");
    }

    



}
