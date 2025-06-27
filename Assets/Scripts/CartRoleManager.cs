using System.Collections.Generic;
using UnityEngine;

public class CartRoleManager : MonoBehaviour
{
    private RoleSwapUI swapUI;
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

        swapUI = GetComponent<RoleSwapUI>();
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

    private void OnRoleSwapStarted(int firstPlayerIndex)
    {
        // Debug.Log("Role swap started"); // one player pressed Y

        if (roleSwapSync.showFeedback && swapUI != null)
        {
            if (firstPlayerIndex < joinedPlayers.Count) {
                var firstPlayer = joinedPlayers[firstPlayerIndex];

                if (firstPlayer.role == CartRole.Driver)
                {
                    swapUI.DriverRequestSwap(true);
                }
                else if (firstPlayer.role == CartRole.Passenger)
                {
                    swapUI.PassengerRequestSwap(true);
                }
            }
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
 
        swapUI.SwapIcons();
        swapUI.Reset();
        Debug.Log("Roles swapped: " + joinedPlayers[0].role + " <-> " + joinedPlayers[1].role);
    }

    private void OnRoleSwapFailed()
    {   
        swapUI.Reset();
        Debug.Log("Role swap sync failed");
    }

    



}
