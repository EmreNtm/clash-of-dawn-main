using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class EnemyShipEvent : NetworkBehaviour
{
    
    private EventSettings.EnemyShipEventSetting enemyShipEventSetting;

    [HideInInspector]
    public float startTime;

    [HideInInspector]
    public List<GameObject> enemyShips;

    [HideInInspector]
    public List<PlayerData> involvedPlayers = new();

    private float sqrEventBorder;

    private void Awake() {
        startTime = Time.time;
        enemyShips = new();
        enemyShipEventSetting = EventManager.Instance.eventSettings.enemyShipEventSetting;
        sqrEventBorder = enemyShipEventSetting.borderRadius + enemyShipEventSetting.borderThickness;
        sqrEventBorder *= sqrEventBorder;
    }

    private void Start() {
        if (!IsServer)
            return;

        int amount = MapManager.serverPrng.Next((int) enemyShipEventSetting.minMaxEnemyAmount.x, (int) enemyShipEventSetting.minMaxEnemyAmount.y);

        StartCoroutine(SpawnEnemyShipsSlowly(amount));
    }

    private void Update() {
        if (!IsServer)
            return;

        // Time condition to end event
        if (false && Time.time > startTime + enemyShipEventSetting.duration) {
            EndEnemyShipEvent();
            return;
        }

        // EnemyShip amount condition to end event
        if (Time.time > startTime + 5f && enemyShips.Count == 0) {
            EndEnemyShipEvent();
            EventManager.Instance.temp = !EventManager.Instance.temp;
            return;
        }

        // Destroy enemyShips if needed.
        GameObject enemyShip;
        bool destroy;
        int j;
        PlayerData pd;
        for (int i = enemyShips.Count - 1; i >= 0; i--) {
            enemyShip = enemyShips[i];

            // If there is an involvedPlayer around the enemyShip, don't destroy the enemyShip.
            destroy = true;
            j = 0;
            while (j < involvedPlayers.Count && destroy) {
                pd = involvedPlayers[j];
                if (Vector3.SqrMagnitude(enemyShip.transform.position - pd.playerShip.transform.position) < sqrEventBorder) {
                    destroy = false;
                }
                j++;
            }

            if (destroy) {
                // enemyShip.GetComponent<NetworkObject>().Despawn();
                ShipGenerator.Instance.DestroyShip(enemyShip);
                enemyShips.RemoveAt(i);
                Debug.Log("Destroyed!");
            }
        }
    }

    private void SpawnEnemyShip() {
        // If no involvedPlayer is around the event area don't spawn an enemyShip.
        bool flag = true;
        int j = 0;
        PlayerData player;
        while (j < involvedPlayers.Count && flag) {
            player = involvedPlayers[j];
            if (Vector3.SqrMagnitude(player.playerShip.transform.position - transform.position) < sqrEventBorder) {
                flag = false;
            }
            j++;
        }
        if (flag)
            return;

        // GameObject enemyShip = Instantiate(enemyShipEventSetting.enemyShipPrefab);
        // Spawn(enemyShip);
        GameObject enemyShip = ShipGenerator.Instance.RequestShips(1, Vector3.zero)[0];

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnEnemyShip(pd.Owner, enemyShip);
        }
    }

    [TargetRpc]
    private void TargetSpawnEnemyShip(NetworkConnection conn, GameObject enemyShip) {
        enemyShip.transform.parent = transform.GetChild(0);

        float offset =((float) MapManager.prng.NextDouble() * 2 * enemyShipEventSetting.borderThickness - enemyShipEventSetting.borderThickness);
        enemyShip.transform.position = transform.position + MapManager.Instance.PointOnUnitSphere() * (enemyShipEventSetting.borderRadius + offset);

        Vector3 velocity = (transform.position - enemyShip.transform.position).normalized;
        enemyShip.transform.rotation = Quaternion.LookRotation(velocity, enemyShip.transform.up);

        velocity = Quaternion.AngleAxis(((float) MapManager.prng.NextDouble() * 10f - 5f), enemyShip.transform.up) * velocity;
        velocity = Quaternion.AngleAxis(((float) MapManager.prng.NextDouble() * 10f - 5f), enemyShip.transform.right) * velocity;
        Rigidbody rb = enemyShip.GetComponent<Rigidbody>();
        // rb.velocity = velocity * ((float) MapManager.prng.NextDouble() * 240f - 120f);
        // rb.angularVelocity = new Vector3(((float) MapManager.prng.NextDouble() * 5 - 2.5f), ((float) MapManager.prng.NextDouble() * 5 - 2.5f), 
        //        ((float) MapManager.prng.NextDouble() * 5f - 2.5f));

        enemyShips.Add(enemyShip);
    }

    public void EndEnemyShipEvent() {
        foreach (PlayerData pd in involvedPlayers) {
            pd.eventInfos.enemyShipEventReadyTime = Time.time + 30f;
            pd.eventInfos.isHavingEnemyShipEvent = false;
        }

        foreach (GameObject enemyShip in enemyShips) {
            // enemyShip.GetComponent<NetworkObject>().Despawn();
            ShipGenerator.Instance.DestroyShip(enemyShip);
        }
        Despawn();
        Debug.Log("EnemyShip Event is Over!");
    }

    private IEnumerator SpawnEnemyShipsSlowly(int amount) {
        for (int i = 0; i < amount; i++) {
            SpawnEnemyShip();
            yield return new WaitForSeconds(0.1f);
        }
    }

}
