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
    public GameObject mapManagerPrefab;
    public GameObject shipPrefab;
    public GameObject planetPrefab;

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

        //MapManager.Instance.CreateSystem(MapManager.Instance.systemSettings.seed);
        // GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        // GameObject go = Instantiate(prefab);
        // Spawn(go, Owner);
        //go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        //go.GetComponent<PlanetObject>().CreatePlanet();

        //GameObject mapManagerPrefab = Addressables.LoadAssetAsync<GameObject>("MapManager").WaitForCompletion();
        GameObject go = Instantiate(mapManagerPrefab);
        Spawn(go);

        for (int i = 0; i < players.Count; i++) {
            players[i].StartGame(players[i].Owner);
            //SetPlanetTransform(players[i].Owner, go);
        }
    }

    [Server]
    public void StopGame() {
        for (int i = 0; i < players.Count; i++) {
            players[i].StopGame();
        }
    }

    [TargetRpc]
    private void SetPlanetTransform(NetworkConnection conn, GameObject go) {
        go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        go.GetComponent<PlanetObject>().CreatePlanet();
    }

        // GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        // GameObject go = Instantiate(prefab);
        // go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        // go.GetComponent<PlanetObject>().CreatePlanet();
        // Spawn(go, Owner);

}
