using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.AddressableAssets;

public sealed class PlayerData : NetworkBehaviour
{

    public static PlayerData Instance { get; private set; }

    [SyncVar]
    public string username;

    [SyncVar]
    public bool isReady;

    [field: SerializeField]
    [field: SyncVar]
    public int score {
        get;

        [ServerRpc]
        private set;
    }

    [SyncVar]
    public Pawn controlledPawn;

    //Called on server
    public override void OnStartServer() {
        base.OnStartServer();

        GameManager.Instance.players.Add(this);
    }

    //Called on client
    public override void OnStartClient() {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;

        ViewManager.Instance.Initialize();
    }

    //Called on server
    public override void OnStopServer() {
        base.OnStopServer();

        GameManager.Instance.players.Remove(this);
    }

    private void Update() {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.R)) {
            ServerSetIsReady(!isReady);
        }
    }

    //We call this from UI and UI is not a NetworkBehaviour
    [ServerRpc(RequireOwnership = false)]
    public void ServerSetIsReady(bool value) {
        isReady = value;
    }

    public void StartGame() {
        GameObject pawnPrefab = Addressables.LoadAssetAsync<GameObject>("Pawn").WaitForCompletion();

        GameObject pawnInstance = Instantiate(pawnPrefab);
        Spawn(pawnInstance, Owner);

        controlledPawn = pawnInstance.GetComponent<Pawn>();
    }

    public void StopGame() {
        if (controlledPawn != null && controlledPawn.IsSpawned) {
            controlledPawn.Despawn();
        }
    }

}
