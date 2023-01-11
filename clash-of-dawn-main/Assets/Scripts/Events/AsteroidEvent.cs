using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class AsteroidEvent : NetworkBehaviour
{
    
    // buna public event settings lazım.
    public GameObject asteroidPrefab;
    public int asteroidAmount;
    public float borderRadius;
    public float duration;

    [HideInInspector]
    public float startTime;

    [HideInInspector]
    public List<GameObject> asteroids;

    // Values server keeps values;
    [HideInInspector]
    public List<PlayerData> involvedPlayers = new();

    private void Awake() {
        startTime = Time.time;
        asteroids = new();
    }

    private void Start() {
        if (!IsServer)
            return;

        GameObject asteroid;
        for (int i = 0; i < asteroidAmount; i++) {
            asteroid = Instantiate(asteroidPrefab);
            Spawn(asteroid);
            foreach (PlayerData pd in GameManager.Instance.players) {
                TargetSpawnAsteroid(pd.Owner, asteroid);
            }
        }
    }

    private void Update() {
        if (!IsServer)
            return;

        // Time condition to end event
        if (Time.time > startTime + duration) {
            foreach (PlayerData pd in involvedPlayers) {
                pd.eventInfos.asteroidEventReadyTime = Time.time + 30f;
                pd.eventInfos.isHavingAsteroidEvent = false;
            }

            foreach (GameObject asteroid in asteroids) {
                asteroid.GetComponent<NetworkObject>().Despawn();
            }
            Despawn();
            Debug.Log("Asteroid Event is Over!");
        }
    }

    [TargetRpc]
    private void TargetSpawnAsteroid(NetworkConnection conn, GameObject asteroid) {
        asteroid.transform.parent = transform.GetChild(0);
        asteroid.GetComponent<PlanetObject>().CreatePlanet();
        asteroid.transform.position = transform.position + MapManager.Instance.PointOnUnitSphere() * 25f;
        asteroids.Add(asteroid);
    }

}
