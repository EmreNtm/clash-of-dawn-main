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

    [field: SerializeField]
    [field: SyncVar]
    public string username {
        get;

        [ServerRpc]
        set;
    }

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
        public float enemyShipEventReadyTime;
        public float ctfEventReadyTime;
        public bool isHavingAsteroidEvent;
        public bool isHavingEnemyShipEvent;
        public bool isHavingCtfEvent;
    }

    public EventInfos eventInfos;

    [field: SerializeField]
    [field: SyncVar]
    public float health {
        get;

        private set;
    }
    private float damageImmuneTime;

    public Vector3 spawnPoint;

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

        if (Input.GetKeyDown(KeyCode.Escape)) {
            ViewManager.Instance.Show<EscView>();
        }
    }

    //We call this from UI and UI is not a NetworkBehaviour
    [ServerRpc(RequireOwnership = false)]
    public void ServerSetIsReady(bool value) {
        isReady = value;
    }

    //Server calls this
    public void StartGame(NetworkConnection conn, int seed) {
        CreateMap(conn, seed);
        health = 100f;
        damageImmuneTime = Time.time;

        //GameObject shipPrefab = Addressables.LoadAssetAsync<GameObject>("Ship").WaitForCompletion();
        GameObject shipInstance = Instantiate(GameManager.Instance.shipPrefab);
        Spawn(shipInstance, Owner);
        shipInstance.GetComponent<MultiplayerShipController>().ServerSetShipSpawnPosition();
        playerShip = shipInstance;

        eventInfos = new EventInfos();
        eventInfos.asteroidEventReadyTime = Time.time + EventManager.Instance.asteroidEventSetting.eventStartingCooldown;
        eventInfos.enemyShipEventReadyTime = Time.time + EventManager.Instance.enemyShipEventSetting.eventStartingCooldown;
        eventInfos.ctfEventReadyTime = Time.time + EventManager.Instance.eventSettings.ctfEventSetting.eventStartingCooldown;
        eventInfos.isHavingAsteroidEvent = false;
        eventInfos.isHavingEnemyShipEvent = false;
        eventInfos.isHavingCtfEvent = false;
    }

    //Server calls this
    public void StopGame() {
        MultiplayerShipController msp = playerShip.GetComponent<MultiplayerShipController>();
        if (msp != null && msp.IsSpawned) {
            msp.Despawn();
        }
    }

    [TargetRpc]
    private void CreateMap(NetworkConnection conn, int seed) {
        MapManager.Instance.CreateSystem(seed);
    }

    [ServerRpc]
    public void DealDamage(float damage) {
        if (damageImmuneTime > Time.time)
            return;

        damageImmuneTime = Time.time + 0.1f;

        health = health - damage < 0 ? 0 : health - damage;
        playerShip.GetComponentInChildren<HealthBar>().UpdateHealthBar(100f, health);
        if (health <= 0)
            DestroyShip();
    }

    [ServerRpc(RequireOwnership=false)]
    public void Respawn() {
        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetRespawn(pd.Owner, playerShip);
        }
    }

    [TargetRpc]
    private void TargetRespawn(NetworkConnection conn, GameObject ship) {
        ship.transform.position = spawnPoint;
        ship.SetActive(true);
    }

    private void DestroyShip() {
        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetDestroyShip(pd.Owner, playerShip);
        }
    }

    [TargetRpc]
    private void TargetDestroyShip(NetworkConnection conn, GameObject ship) {
        ship.SetActive(false);
        ViewManager.Instance.Show<RespawnView>();
    }

}
