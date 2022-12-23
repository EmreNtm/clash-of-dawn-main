using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;

public sealed class GameManager : NetworkBehaviour
{
    
    public static GameManager Instance { get; private set; }

    [SyncObject]
    public readonly SyncList<PlayerData> players = new();

    [SyncVar]
    public bool canStart;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        if (!IsServer)
            return;

        canStart = players.All(player => player.isReady);
    }

    [Server]
    public void StartGame() {
        if (!canStart)
            return;

        for (int i = 0; i < players.Count; i++) {
            players[i].StartGame();
        }
    }

    [Server]
    public void StopGame() {
        for (int i = 0; i < players.Count; i++) {
            players[i].StopGame();
        }
    }

}
