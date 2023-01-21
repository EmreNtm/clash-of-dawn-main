using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class CtfEvent : NetworkBehaviour
{
    private EventSettings.CtfEventSetting ctfEventSetting;

    [HideInInspector]
    public float startTime;

    [HideInInspector]
    public List<GameObject> enemyShips;
    private int enemyShipAmount;
    private bool didFinishFirstSpawnGroup = false;

    [HideInInspector]
    public List<PlayerData> involvedPlayers = new();

    private float sqrEventBorder;

    [HideInInspector]
    public CTFManager ctfManager;
    [HideInInspector]
    public CtfProgressBar cpb;


    private Transform targetTransform;
    private float captureProgress = 0f;
    private float targetRadius;
    private float sqrTargetRadius;

    private void Awake() {
        startTime = Time.time;
        enemyShips = new();
        ctfEventSetting = EventManager.Instance.eventSettings.ctfEventSetting;
        sqrEventBorder = ctfEventSetting.borderRadius + ctfEventSetting.borderThickness;
        sqrEventBorder *= sqrEventBorder;
    }

    private void Start() {
        if (!IsServer)
            return;

        enemyShipAmount = MapManager.serverPrng.Next((int) ctfEventSetting.minMaxEnemyAmount.x, (int) ctfEventSetting.minMaxEnemyAmount.y);

        StartCoroutine(SpawnEnemyShipsSlowly(enemyShipAmount));
    }

    private void Update() {
        if (!IsServer)
            return;

        // // Time condition to end event
        // if (Time.time > startTime + ctfEventSetting.duration) {
        //     EndCtfEvent();
        //     return;
        // }

        // // EnemyShip amount condition to end event
        // if (Time.time > startTime + 5f && enemyShips.Count == 0) {
        //     EndCtfEvent();
        //     // EventManager.Instance.temp = !EventManager.Instance.temp;
        //     return;
        // }
    }

    private void FixedUpdate() {
        if (!IsServer)
            return;

        if (targetTransform == null)
            return;

        // Check if all the players still near the station
        bool flag = true;
        foreach (PlayerData pd in involvedPlayers) {
            if (Vector3.SqrMagnitude(pd.playerShip.transform.position - transform.position) < sqrEventBorder) {
                flag = false;
            }
        }
        if (flag) {
            EndCtfEvent(false);
            return;
        }

        // Capture the Flag
        UpdateCtf();
        if (captureProgress >= 1) {
            EndCtfEvent(true);
            return;
        }

        // Replace enemyShips if needed.
        ReplaceEnemyShipsFarAway();

        // // Bring more ships if any ship is missing
        // if (didFinishFirstSpawnGroup && enemyShips.Count < enemyShipAmount)
        //     SpawnEnemyShip();
    }

    private void OnDrawGizmos() {
        if (targetTransform == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetTransform.position, targetRadius);
    }

    private void UpdateCtf() {
        Debug.Log("Progress: " + captureProgress);
        captureProgress -= ctfEventSetting.captureProgressInterval * Time.fixedDeltaTime / 4f;
        captureProgress = captureProgress < 0 ? 0 : captureProgress;
        foreach (PlayerData pd in involvedPlayers) {
            if (Vector3.SqrMagnitude(pd.playerShip.transform.position - targetTransform.position) < sqrTargetRadius) {
                captureProgress += ctfEventSetting.captureProgressInterval * Time.fixedDeltaTime;
                if (captureProgress >= 1)
                    Debug.Log("Captured!");
            }
        }
        cpb.UpdateProgressBar(captureProgress);
    }

    private void ReplaceEnemyShipsFarAway() {
        GameObject enemyShip;
        bool replace;
        int j;
        PlayerData pd;
        for (int i = enemyShips.Count - 1; i >= 0; i--) {
            enemyShip = enemyShips[i];

            // If there is an involvedPlayer around the enemyShip, don't destroy the enemyShip.
            replace = true;
            j = 0;
            while (j < involvedPlayers.Count && replace) {
                pd = involvedPlayers[j];
                if (Vector3.SqrMagnitude(enemyShip.transform.position - pd.playerShip.transform.position) < sqrEventBorder) {
                    replace = false;
                }
                j++;
            }

            if (replace) {
                ShipGenerator.Instance.DestroyShip(enemyShip);
                enemyShips.RemoveAt(i);
                SpawnEnemyShip();
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

        GameObject enemyShip = ShipGenerator.Instance.RequestShips(1, Vector3.zero)[0];

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnEnemyShip(pd.Owner, enemyShip);
        }
    }

    [TargetRpc]
    private void TargetSpawnEnemyShip(NetworkConnection conn, GameObject enemyShip) {
        enemyShip.transform.parent = transform.GetChild(0);

        float offset =((float) MapManager.prng.NextDouble() * 2 * ctfEventSetting.borderThickness - ctfEventSetting.borderThickness);
        enemyShip.transform.position = transform.position + MapManager.Instance.PointOnUnitSphere() * (ctfEventSetting.borderRadius + offset);

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

    public void EndCtfEvent(bool wasCtfSuccesfull) {
        ctfManager.isCtfActive = false;
        ctfManager.isHostile = !wasCtfSuccesfull;

        foreach (PlayerData pd in involvedPlayers) {
            pd.eventInfos.isHavingCtfEvent = false;
        }

        foreach (GameObject enemyShip in enemyShips) {
            ShipGenerator.Instance.DestroyShip(enemyShip);
        }
        Despawn();
        Debug.Log("Ctf Event is Over!");
    }

    private IEnumerator SpawnEnemyShipsSlowly(int amount) {
        for (int i = 0; i < amount; i++) {
            SpawnEnemyShip();
            yield return new WaitForSeconds(0.1f);
        }
        didFinishFirstSpawnGroup = true;
    }

    public void SetTargetTransform(Transform tf) {
        this.targetTransform = tf;
        this.targetRadius = ctfEventSetting.flagAreaRadius;
        this.sqrTargetRadius = this.targetRadius * this.targetRadius;
    }
}
