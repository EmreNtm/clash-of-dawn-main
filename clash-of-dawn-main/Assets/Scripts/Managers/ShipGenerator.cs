using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class ShipGenerator : NetworkBehaviour
{

    public static ShipGenerator Instance { get; private set; }

    private List<GameObject> availableShips;
    private List<GameObject> aliveShips;
    public GameObject enemyShipPrefab;

    private void Awake() {
        Instance = this;
        availableShips = new();
        aliveShips = new();
    }

    public void Initialize() {
        if (!IsServer)
            return;

        for (int i = 0; i < GameManager.Instance.gameSettings.maxEnemyAmount; i++) {
            GameObject enemyShip = Instantiate(enemyShipPrefab);
            enemyShip.SetActive(false);
            Spawn(enemyShip);
            availableShips.Add(enemyShip);

            foreach (PlayerData pd in GameManager.Instance.players) {
                TargetSpawnEnemyShip(pd.Owner, enemyShip);
            }
        }
    }

    [TargetRpc]
    private void TargetSpawnEnemyShip(NetworkConnection conn, GameObject enemyShip) {
        enemyShip.gameObject.SetActive(false);
        enemyShip.transform.parent = transform.GetChild(0);
    }

    public GameObject[] RequestShips(int amount, Vector3 pos) {
        amount = amount > availableShips.Count ? availableShips.Count : amount;
        GameObject ship;
        GameObject[] ships = new GameObject[amount];
        int lastIndex = availableShips.Count - amount;
        int j = 0;
        for (int i = availableShips.Count - 1; i >= lastIndex; i--) {
            ship = availableShips[i];
            ship.transform.position = pos;
            ship.gameObject.SetActive(true);
            aliveShips.Add(ship);
            availableShips.RemoveAt(i);
            ships[j++] = ship;
        }

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetRequestShips(pd.Owner, ships, pos);
        }

        return ships;
    }

    public void DestroyShip(GameObject ship) {
        ship.gameObject.SetActive(false);
        if (aliveShips.Contains(ship))
            aliveShips.Remove(ship);
        if (!availableShips.Contains(ship)) {
            ship.transform.parent = transform.GetChild(0);
            availableShips.Add(ship);
        }

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetDestroyShip(pd.Owner, ship);
        }
    }

    [TargetRpc]
    private void TargetDestroyShip(NetworkConnection conn, GameObject ship) {
        ship.gameObject.SetActive(false);
        ship.transform.parent = transform.GetChild(0);
    }

    [TargetRpc]
    private void TargetRequestShips(NetworkConnection conn, GameObject[] ships, Vector3 pos) {
        for (int i = 0; i < ships.Length; i++) {
            ships[i].transform.position = pos;
            ships[i].gameObject.SetActive(true);
        }
    }

}
