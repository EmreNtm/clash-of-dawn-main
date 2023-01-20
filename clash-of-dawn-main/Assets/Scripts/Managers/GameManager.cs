using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine.AddressableAssets;
using FishNet.Connection;

public sealed class GameManager : NetworkBehaviour
{
    
    public static GameManager Instance { get; private set; }

    public GameSettings gameSettings;

    // Spawnable prefabs.
    public GameObject mapManagerPrefab;
    public GameObject shipPrefab;

    // Players in the game/lobby.
    [SyncObject]
    public readonly SyncList<PlayerData> players = new();

    // Is all players ready for the game to start.
    [SyncVar]
    public bool canStart;

    [SyncVar]
    public bool started = false;

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

        GameObject go = Instantiate(mapManagerPrefab);
        Spawn(go);

        ShipGenerator.Instance.Initialize();

        for (int i = 0; i < players.Count; i++) {
            players[i].StartGame(players[i].Owner);
        }

        started = true;
    }

    [Server]
    public void StopGame() {
        for (int i = 0; i < players.Count; i++) {
            players[i].StopGame();
        }
    }

        // GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        // GameObject go = Instantiate(prefab);
        // go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        // go.GetComponent<PlanetObject>().CreatePlanet();
        // Spawn(go, Owner);

}
