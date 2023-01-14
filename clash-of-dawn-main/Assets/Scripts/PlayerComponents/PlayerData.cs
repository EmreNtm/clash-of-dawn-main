using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.AddressableAssets;
using FishNet.Connection;

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
    public GameObject playerShip;

    public struct EventInfos {

        public float asteroidEventReadyTime;
        public float EnemyShipEventReadyTime;
        public bool isHavingAsteroidEvent;
        public bool isHavingEnemyShipEvent;

    }

    public EventInfos eventInfos;

    [field: SerializeField]
    [field: SyncVar]
    public float health {
        get;

        private set;
    }
    private float damageImmuneTime;

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
        name = IsServer.ToString();

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

    //Server calls this
    public void StartGame(NetworkConnection conn) {
        CreateMap(conn);
        health = 100f;
        damageImmuneTime = Time.time;

        //GameObject shipPrefab = Addressables.LoadAssetAsync<GameObject>("Ship").WaitForCompletion();
        GameObject shipInstance = Instantiate(GameManager.Instance.shipPrefab);
        Spawn(shipInstance, Owner);
        playerShip = shipInstance;

        eventInfos = new EventInfos();
        eventInfos.asteroidEventReadyTime = Time.time + EventManager.Instance.asteroidEventSetting.eventStartingCooldown;
        eventInfos.EnemyShipEventReadyTime = Time.time + EventManager.Instance.EnemyShipEventStartingCooldown;
        eventInfos.isHavingAsteroidEvent = false;
        eventInfos.isHavingEnemyShipEvent = false;
    }

    //Server calls this
    public void StopGame() {
        MultiplayerShipController msp = playerShip.GetComponent<MultiplayerShipController>();
        if (msp != null && msp.IsSpawned) {
            msp.Despawn();
        }
    }

    [TargetRpc]
    private void CreateMap(NetworkConnection conn) {
        MapManager.Instance.CreateSystem(MapManager.Instance.systemSettings.seed);
    }

    [ServerRpc]
    public void DealDamage(float damage) {
        if (damageImmuneTime > Time.time)
            return;

        damageImmuneTime = Time.time + 0.1f;

        health = health - damage < 0 ? 0 : health - damage;
        Debug.Log("deal damage health: " + health);
    }

}
